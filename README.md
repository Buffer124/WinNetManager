Markdown
# WinNetManager
**WinNetManager** là công cụ quản lý cấu hình mạng trên Windows (GUI / CLI), giúp đơn giản hóa việc chuyển đổi cấu hình IP, quản lý DNS, bật/tắt adapter và kiểm tra kết nối.
---
## 🚀 Tính Năng Chính

- **Quản lý Adapter Mạng:** Liệt kê, bật/tắt nhanh các card mạng (Ethernet, Wi-Fi, Virtual Adapter).
- **Chuyển Đổi IP Linh Hoạt:** Đổi nhanh giữa DHCP (IP động) và Static IP (IP tĩnh) theo cấu hình lưu sẵn (profiles).
- **Cấu Hình DNS Nhanh:** Hỗ trợ các DNS phổ biến (Google, Cloudflare, OpenDNS, Quad9) chỉ với 1 click hoặc 1 lệnh.
- **Công Cụ Chẩn Đoán:** Tích hợp kiểm tra Ping, Traceroute, Flush DNS (`ipconfig /flushdns`), và reset TCP/IP.
- **Xuất / Nhập Cấu Hình:** Lưu cấu hình hiện tại ra tệp (JSON/YAML) để sao lưu hoặc triển khai hàng loạt.

---

## 📋 Yêu Cầu Hệ Thống

- **Hệ điều hành:** Windows 10 / Windows 11 / Windows Server (64-bit).
- **Quyền hạn:** Cần quyền Quản trị viên (**Run as Administrator**) để thay đổi cấu hình mạng.
- **Dependencies (nếu có):** .NET Framework 4.8 / .NET 8 / PowerShell 5.1+.

---

## 🛠️ Cài Đặt
### Cách 1: Tải Bản Phát Hành (Release)
1. Vào mục [Releases](https://github.com/your-username/WinNetManager/releases).
2. Tải về tệp nén mới nhất (`WinNetManager-vX.X.zip`).
3. Giải nén vào thư mục mong muốn.

### Cách 2: Biên Dịch Từ Mã Nguồn (Build from source)
```bash
git clone [https://github.com/your-username/WinNetManager.git](https://github.com/your-username/WinNetManager.git)
cd WinNetManager
# Ví dụ build bằng .NET CLI
dotnet build --configuration Release
📖 Hướng Dẫn Sử Dụng
Lưu ý: Luôn mở ứng dụng bằng Run as Administrator.

 Sử Dụng Qua Giao Diện (GUI)
Chọn card mạng cần thao tác từ danh sách thả xuống.
Nhập thông tin IP/Subnet/Gateway/DNS hoặc chọn profile định sẵn.
Nhấn Apply để lưu thay đổi.

🤝 Đóng Góp (Contributing)
Mọi đóng góp nhằm cải thiện dự án đều được hoan nghênh:
Fork dự án.
Tạo nhánh mới (git checkout -b feature/tinh-nang-moi).
Commit thay đổi (git commit -m 'Thêm tính năng...').
Push nhánh (git push origin feature/tinh-nang-moi).
Tạo Pull Request.

📄 Bản Quyền (License)
Dự án được phát hành dưới giấy phép MIT License.
---
Dự án WinNetManager của bạn được viết bằng ngôn ngữ nào (C#, C++, Python, hay PowerShell)
và có giao diện đồ họa (GUI) hay dòng lệnh (CLI)? Hãy chia sẻ thêm để README được tùy chỉnh sát nhất với tính năng thực tế.

# WinNetManager

A lightweight, high-performance network management utility for Windows, designed to simplify adapter configuration, IP switching, DNS management, and network troubleshooting.

---

## 🚀 Key Features

* **Network Adapter Control:** Easily view, enable, disable, and monitor network interfaces (Ethernet, Wi-Fi, virtual adapters).
* **Flexible IP Configuration:** Quickly toggle between Dynamic (DHCP) and Static IP profiles.
* **Fast DNS Switcher:** Pre-configured with popular secure DNS providers (Cloudflare, Google, OpenDNS, Quad9).
* **Diagnostic & Repair Tools:** Built-in one-click tools for Ping, Traceroute, DNS cache flush (`ipconfig /flushdns`), and TCP/IP stack reset.
* **Native & Portable:** Standalone executable with zero external runtime dependencies.

---

## 📋 System Requirements

* **Operating System:** Windows 10 / 11 / Windows Server (32-bit & 64-bit).
* **Privileges:** Requires Administrator rights (**Run as Administrator**) to modify network adapter settings and system services.

---

## 🛠️ Installation & Usage

1. Download the latest release from the [Releases](https://github.com/Buffer124/WinNetManager/releases) page.
2. Extract the archive (if applicable) or run `WinNetManager.exe` directly.
3. Make sure to launch the application with **Run as Administrator**.

---

## 🔒 Security & Integrity

* Built-in system command hardening.
* Tamper detection and runtime integrity checks.
* Authenticode digital signature verification.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
