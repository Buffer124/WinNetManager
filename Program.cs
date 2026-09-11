using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace WinNetManager;

static unsafe class Program
{
    private const string WindowClassName = "WNetMgrInternalClass";
    private static nint hComboAdapters;
    private static nint hEditIp, hEditSubnet, hEditGateway, hEditDns1, hEditDns2;
    private static nint hRadioDhcp, hRadioStatic;
    private static nint hBtnApply, hBtnFlush;
    private static nint hComboDnsPreset;

    private static readonly List<NetworkAdapterInfo> Adapters = [];

    private static readonly (string Name, string Dns1, string Dns2)[] DnsPresets =
    [
        ("Google Public DNS", "8.8.8.8", "8.8.4.4"),
        ("Cloudflare DNS", "1.1.1.1", "1.0.0.1"),
        ("Quad9 Secure DNS", "9.9.9.9", "149.112.112.112"),
        ("OpenDNS Home", "208.67.222.222", "208.67.220.220")
    ];

    #region Win32 API Imports
    [DllImport("kernel32.dll")]
    private static extern bool IsDebuggerPresent();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, void* lpParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetMessageW(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(in MSG lpMsg);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint DispatchMessageW(in MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(in WNDCLASSEX lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetWindowTextW(nint hWnd, string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(nint hWnd, char[] lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(nint hWnd, string lpText, string lpCaption, uint uType);

    [DllImport("user32.dll")]
    private static extern bool EnableWindow(nint hWnd, bool bEnable);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateFontW(int cHeight, int cWidth, int cEscapement, int cOrientation, int cWeight, uint bItalic, uint bUnderline, uint bStrikeOut, uint iCharSet, uint iOutPrecision, uint iClipPrecision, uint iQuality, uint iPitchAndFamily, string pszFaceName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint hWnd, uint Msg, nint wParam, string lParam);

    [DllImport("comctl32.dll")]
    private static extern void InitCommonControls();
    #endregion

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG { public nint hwnd; public uint message; public nint wParam; public nint lParam; public uint time; public POINT pt; }

    private delegate nint WndProcDel(nint hWnd, uint msg, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    private static readonly WndProcDel StaticWndProc = CustomWndProc;

    [STAThread]
    static void Main()
    {
        try
        {
            if (IsDebuggerPresent()) return;

            // Kiem tra quyen Administrator
            if (!IsRunAsAdmin())
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = Environment.ProcessPath ?? "",
                        Verb = "runas"
                    });
                }
                catch { }
                return;
            }

            InitCommonControls();

            WNDCLASSEX wc = new()
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(StaticWndProc),
                lpszClassName = WindowClassName,
                hbrBackground = (nint)16 // COLOR_BTNFACE
            };
            RegisterClassExW(in wc);

            nint hWnd = CreateWindowExW(
                0,
                WindowClassName,
                "Quản Lý Card Mạng Chuyên Nghiệp (WinNetManager)",
                0x00CA0000 | 0x10000000,
                400, 200, 520, 480,
                0, 0, 0, null);

            nint hFont = CreateFontW(16, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
            nint hBold = CreateFontW(16, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");

            CreateLabel(hWnd, "Card Mạng:", 20, 18, 90, 20, hBold);
            hComboAdapters = CreateWindowExW(0, "COMBOBOX", "", 0x50000000 | 0x0003 | 0x0200, 120, 14, 360, 200, hWnd, (nint)101, 0, null);
            SendMessageW(hComboAdapters, 0x0030, hFont, 1);

            hRadioDhcp = CreateWindowExW(0, "BUTTON", "Sử dụng IP Tự Động (DHCP)", 0x50000000 | 0x0009, 20, 50, 220, 24, hWnd, (nint)102, 0, null);
            SendMessageW(hRadioDhcp, 0x0030, hFont, 1);

            hRadioStatic = CreateWindowExW(0, "BUTTON", "Sử dụng IP Tĩnh (Static IP)", 0x50000000 | 0x0009, 260, 50, 220, 24, hWnd, (nint)103, 0, null);
            SendMessageW(hRadioStatic, 0x0030, hFont, 1);

            CreateLabel(hWnd, "Địa chỉ IP:", 25, 90, 100, 20, hFont);
            hEditIp = CreateInput(hWnd, 130, 88, 220, 24, (nint)201, hFont);

            CreateLabel(hWnd, "Subnet Mask:", 25, 125, 100, 20, hFont);
            hEditSubnet = CreateInput(hWnd, 130, 123, 220, 24, (nint)202, hFont);

            CreateLabel(hWnd, "Default Gateway:", 25, 160, 105, 20, hFont);
            hEditGateway = CreateInput(hWnd, 130, 158, 220, 24, (nint)203, hFont);

            CreateLabel(hWnd, "Mẫu DNS nhanh:", 25, 205, 105, 20, hBold);
            hComboDnsPreset = CreateWindowExW(0, "COMBOBOX", "", 0x50000000 | 0x0003 | 0x0200, 130, 201, 220, 200, hWnd, (nint)104, 0, null);
            SendMessageW(hComboDnsPreset, 0x0030, hFont, 1);
            foreach (var p in DnsPresets)
            {
                SendMessageW(hComboDnsPreset, 0x0143, 0, p.Name);
            }

            CreateLabel(hWnd, "Preferred DNS:", 25, 245, 100, 20, hFont);
            hEditDns1 = CreateInput(hWnd, 130, 243, 220, 24, (nint)204, hFont);

            CreateLabel(hWnd, "Alternate DNS:", 25, 280, 100, 20, hFont);
            hEditDns2 = CreateInput(hWnd, 130, 278, 220, 24, (nint)205, hFont);

            hBtnApply = CreateWindowExW(0, "BUTTON", "Áp Dụng Cấu Hình", 0x50000000 | 0x0001, 70, 335, 175, 40, hWnd, (nint)301, 0, null);
            SendMessageW(hBtnApply, 0x0030, hBold, 1);

            hBtnFlush = CreateWindowExW(0, "BUTTON", "Flush DNS & Reset", 0x50000000, 270, 335, 175, 40, hWnd, (nint)302, 0, null);
            SendMessageW(hBtnFlush, 0x0030, hBold, 1);

            // Doc danh sach card mang
            LoadNetworkAdapters();
            if (Adapters.Count > 0)
            {
                SendMessageW(hComboAdapters, 0x014E, 0, 0);
                RefreshAdapterUI(0);
            }

            ShowWindow(hWnd, 1);
            UpdateWindow(hWnd);

            while (GetMessageW(out MSG msg, 0, 0, 0) > 0)
            {
                TranslateMessage(in msg);
                DispatchMessageW(in msg);
            }
        }
        catch (Exception ex)
        {
            MessageBoxW(nint.Zero, "Lỗi khởi chạy ứng dụng:\n" + ex.ToString(), "Lỗi Nghiêm Trọng", 0x10);
        }
    }

    private static nint CustomWndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case 0x0111:
                int cmdId = (int)wParam & 0xFFFF;
                int notif = (int)(wParam >> 16);

                if (cmdId == 101 && notif == 1) // Thay doi lua chon adapter
                {
                    int sel = (int)SendMessageW(hComboAdapters, 0x0147, 0, 0);
                    RefreshAdapterUI(sel);
                }
                else if (cmdId == 102) // Chon DHCP
                {
                    ToggleInputs(false);
                }
                else if (cmdId == 103) // Chon Static IP
                {
                    ToggleInputs(true);
                }
                else if (cmdId == 104 && notif == 1) // Chon DNS Mau
                {
                    int selDns = (int)SendMessageW(hComboDnsPreset, 0x0147, 0, 0);
                    if (selDns >= 0 && selDns < DnsPresets.Length)
                    {
                        SetWindowTextW(hEditDns1, DnsPresets[selDns].Dns1);
                        SetWindowTextW(hEditDns2, DnsPresets[selDns].Dns2);
                    }
                }
                else if (cmdId == 301) // Nut Ap Dung
                {
                    ApplyConfiguration(hWnd);
                }
                else if (cmdId == 302) // Nut Flush DNS
                {
                    FlushDnsNetwork(hWnd);
                }
                break;

            case 0x0010:
            case 0x0002:
                PostQuitMessage(0);
                break;

            default:
                return DefWindowProcW(hWnd, msg, wParam, lParam);
        }
        return 0;
    }

    private static void LoadNetworkAdapters()
    {
        Adapters.Clear();
        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE");
        foreach (ManagementObject obj in searcher.Get())
        {
            string desc = obj["Description"]?.ToString() ?? "Unknown";
            string mac = obj["MACAddress"]?.ToString() ?? "";
            string[]? ips = (string[]?)obj["IPAddress"];
            string[]? subnets = (string[]?)obj["IPSubnet"];
            string[]? gateways = (string[]?)obj["DefaultIPGateway"];
            string[]? dns = (string[]?)obj["DNSServerSearchOrder"];
            bool dhcp = (bool)(obj["DHCPEnabled"] ?? false);
            uint index = (uint)(obj["Index"] ?? 0);

            Adapters.Add(new NetworkAdapterInfo
            {
                Description = desc,
                Index = index,
                IsDhcp = dhcp,
                Ip = ips?.FirstOrDefault() ?? "",
                Subnet = subnets?.FirstOrDefault() ?? "",
                Gateway = gateways?.FirstOrDefault() ?? "",
                Dns1 = dns?.ElementAtOrDefault(0) ?? "",
                Dns2 = dns?.ElementAtOrDefault(1) ?? ""
            });

            SendMessageW(hComboAdapters, 0x0143, 0, $"{desc} ({mac})");
        }
    }

    private static void RefreshAdapterUI(int index)
    {
        if (index < 0 || index >= Adapters.Count) return;
        var a = Adapters[index];

        SetWindowTextW(hEditIp, a.Ip);
        SetWindowTextW(hEditSubnet, string.IsNullOrEmpty(a.Subnet) ? "255.255.255.0" : a.Subnet);
        SetWindowTextW(hEditGateway, a.Gateway);
        SetWindowTextW(hEditDns1, a.Dns1);
        SetWindowTextW(hEditDns2, a.Dns2);

        if (a.IsDhcp)
        {
            SendMessageW(hRadioDhcp, 0x00F1, (nint)1, 0); // BST_CHECKED
            SendMessageW(hRadioStatic, 0x00F1, 0, 0);
            ToggleInputs(false);
        }
        else
        {
            SendMessageW(hRadioStatic, 0x00F1, (nint)1, 0);
            SendMessageW(hRadioDhcp, 0x00F1, 0, 0);
            ToggleInputs(true);
        }
    }

    private static void ToggleInputs(bool enableStatic)
    {
        EnableWindow(hEditIp, enableStatic);
        EnableWindow(hEditSubnet, enableStatic);
        EnableWindow(hEditGateway, enableStatic);
    }

    private static void ApplyConfiguration(nint hWnd)
    {
        int sel = (int)SendMessageW(hComboAdapters, 0x0147, 0, 0);
        if (sel < 0 || sel >= Adapters.Count) return;
        var a = Adapters[sel];

        bool isDhcp = (int)SendMessageW(hRadioDhcp, 0x00F0, 0, 0) == 1;
        string ip = GetText(hEditIp);
        string subnet = GetText(hEditSubnet);
        string gateway = GetText(hEditGateway);
        string dns1 = GetText(hEditDns1);
        string dns2 = GetText(hEditDns2);

        EnableWindow(hBtnApply, false);
        SetWindowTextW(hBtnApply, "Đang thiết lập...");

        new Thread(() =>
        {
            try
            {
                using var obj = new ManagementObject($"Win32_NetworkAdapterConfiguration.Index='{a.Index}'");

                if (isDhcp)
                {
                    obj.InvokeMethod("EnableDHCP", null, null);
                    obj.InvokeMethod("SetDNSServerSearchOrder", null, null);
                }
                else
                {
                    var ipParams = obj.GetMethodParameters("EnableStatic");
                    ipParams["IPAddress"] = new string[] { ip };
                    ipParams["SubnetMask"] = new string[] { subnet };
                    obj.InvokeMethod("EnableStatic", ipParams, null);

                    if (!string.IsNullOrWhiteSpace(gateway))
                    {
                        var gwParams = obj.GetMethodParameters("SetGateways");
                        gwParams["DefaultIPGateway"] = new string[] { gateway };
                        obj.InvokeMethod("SetGateways", gwParams, null);
                    }
                }

                List<string> dnsList = [];
                if (!string.IsNullOrWhiteSpace(dns1)) dnsList.Add(dns1.Trim());
                if (!string.IsNullOrWhiteSpace(dns2)) dnsList.Add(dns2.Trim());

                if (dnsList.Count > 0)
                {
                    var dnsParams = obj.GetMethodParameters("SetDNSServerSearchOrder");
                    dnsParams["DNSServerSearchOrder"] = dnsList.ToArray();
                    obj.InvokeMethod("SetDNSServerSearchOrder", dnsParams, null);
                }

                MessageBoxW(hWnd, "Cấu hình card mạng thành công!", "Thông Báo", 0x40);
            }
            catch (Exception ex)
            {
                MessageBoxW(hWnd, "Lỗi khi cấu hình:\n" + ex.Message, "Lỗi", 0x10);
            }
            finally
            {
                EnableWindow(hBtnApply, true);
                SetWindowTextW(hBtnApply, "Áp Dụng Cấu Hình");
            }
        })
        { IsBackground = true }.Start();
    }

    private static void FlushDnsNetwork(nint hWnd)
    {
        EnableWindow(hBtnFlush, false);
        SetWindowTextW(hBtnFlush, "Đang làm mới...");

        new Thread(() =>
        {
            ExecuteCmd("ipconfig", "/flushdns");
            ExecuteCmd("ipconfig", "/release");
            ExecuteCmd("ipconfig", "/renew");
            EnableWindow(hBtnFlush, true);
            SetWindowTextW(hBtnFlush, "Flush DNS & Reset");
            MessageBoxW(hWnd, "Đã làm sạch bộ đệm DNS và làm mới IP thành công!", "Thông Báo", 0x40);
        })
        { IsBackground = true }.Start();
    }

    private static void ExecuteCmd(string app, string args)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo(app, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            proc?.WaitForExit(6000);
        }
        catch { }
    }

    private static nint CreateLabel(nint parent, string text, int x, int y, int w, int h, nint font)
    {
        nint hWnd = CreateWindowExW(0, "STATIC", text, 0x50000000, x, y, w, h, parent, 0, 0, null);
        SendMessageW(hWnd, 0x0030, font, 1);
        return hWnd;
    }

    private static nint CreateInput(nint parent, int x, int y, int w, int h, nint id, nint font)
    {
        nint hWnd = CreateWindowExW(0x00000200, "EDIT", "", 0x50000000 | 0x0080, x, y, w, h, parent, id, 0, null);
        SendMessageW(hWnd, 0x0030, font, 1);
        return hWnd;
    }

    private static string GetText(nint hWnd)
    {
        char[] buf = new char[256];
        int len = GetWindowTextW(hWnd, buf, buf.Length);
        return new string(buf, 0, len);
    }

    private static bool IsRunAsAdmin()
    {
        try
        {
            WindowsPrincipal p = new(WindowsIdentity.GetCurrent());
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    private class NetworkAdapterInfo
    {
        public string Description { get; set; } = "";
        public uint Index { get; set; }
        public bool IsDhcp { get; set; }
        public string Ip { get; set; } = "";
        public string Subnet { get; set; } = "";
        public string Gateway { get; set; } = "";
        public string Dns1 { get; set; } = "";
        public string Dns2 { get; set; } = "";
    }
}