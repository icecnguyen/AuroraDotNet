# AuroraDotNet
[English](#english) | [Tiếng Việt](#tiếng-việt)

---

<a name="english"></a>
## 🇬🇧 English

### Overview
**AuroraDotNet** is an experimental, high-performance Minecraft server implementation written entirely in C# (.NET 9.0). Designed from the ground up to support the modern Minecraft 1.21.4 (Protocol 768) specification, this project aims to explore the capabilities of zero-allocation networking and high-throughput memory management in C#.

### ✨ Key Features
- **Modern Protocol (Minecraft 1.21.4):** Implements the strict `Configuration` state handshake (Feature Flags, Known Packs, Registry Data).
- **High-Performance I/O:** Built on top of `System.IO.Pipelines`, `ReadOnlySequence`, and `Span<T>` to ensure extremely fast packet parsing with virtually zero heap allocations.
- **Integrated Web Dashboard:** A sleek, built-in glassmorphism web panel (runs on port `5000`) for real-time monitoring of CPU/RAM, managing players, and viewing live server console logs.
- **Multiplayer Sync:** Fully functional multiplayer synchronization. See other players move, look around, break/place blocks, and chat in real-time.
- **Core Mechanics:** Dynamic chunk generation (Anvil structure mapping), global chat, block breaking, and block placement.

### 🚀 Getting Started

1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
2. Clone the repository and navigate to the project directory.
3. Build the project:
   ```bash
   dotnet build -c Release
   ```
4. Run the server:
   ```bash
   dotnet run --project src/Aurora.Bootstrap -c Release
   ```

### 🎮 How to Connect
- **Locally:** Open Minecraft 1.21.4, go to Multiplayer, add a server with IP `localhost:25565` (or `127.0.0.1`), and join!
- **Play with Friends:** Since the server listens on `0.0.0.0:25565`, you can easily use tunneling services like [playit.gg](https://playit.gg/) or ngrok to expose your server to the internet without port forwarding.
- **Web Dashboard:** Open your browser and navigate to `http://localhost:5000` to access the server management panel.

### 🗺 Roadmap
- [x] Basic networking and Handshake state.
- [x] Login and modern Configuration state flow (Vanilla Registry NBT Sync).
- [x] **Core Mechanics**: World/Chunk Generation (Anvil format), Chat, Block breaking/placing.
- [x] **Multiplayer Sync**: Player Info Updates, Entity Spawning, and Position/Look broadcasting.
- [ ] **Inventory System**: Hotbar syncing, item interactions, and entity metadata.
- [ ] **Physics Engine**: Bounding box validation, falling, and entity ticking.

### 🤝 Credits & Acknowledgements
- **Author/Creator:** icecnguyen
- **AI Assistance:** A significant portion of this project's initial scaffolding, debugging, and protocol reverse-engineering was pair-programmed alongside **Antigravity (Google DeepMind)**.
- **Tools & Libraries:** 
  - Protocol references heavily sourced from [wiki.vg](https://wiki.vg/) and [PrismarineJS/minecraft-data](https://github.com/PrismarineJS/minecraft-data).
  - Uses `xUnit` for comprehensive bitwise and spatial mathematical testing.

If you found this project helpful or interesting, consider supporting the development by giving this repository a ⭐ **Star**!

---

<a name="tiếng-việt"></a>
## 🇻🇳 Tiếng Việt

### Tổng quan
**AuroraDotNet** là một dự án máy chủ Minecraft thử nghiệm với hiệu suất cao, được viết hoàn toàn bằng ngôn ngữ C# (trên nền tảng .NET 9.0). Được thiết kế để hỗ trợ đặc tả giao thức của Minecraft 1.21.4 (Protocol 768), dự án này tập trung vào việc khai phá sức mạnh của các kỹ thuật quản lý bộ nhớ không cấp phát (zero-allocation) và băng thông mạng cực cao trong C#.

### ✨ Các tính năng nổi bật
- **Giao thức hiện đại (Minecraft 1.21.4):** Tuân thủ chặt chẽ quá trình handshake khắt khe của state `Configuration` (Feature Flags, Known Packs, Registry Data).
- **I/O Hiệu suất cao:** Xây dựng dựa trên `System.IO.Pipelines`, `ReadOnlySequence`, và `Span<T>` để đảm bảo tốc độ phân tích packet cực nhanh và hầu như không xả rác bộ nhớ.
- **Bảng điều khiển Web (Web Dashboard):** Tích hợp sẵn một trang quản trị giao diện Glassmorphism tuyệt đẹp (chạy tại cổng `5000`). Cho phép bạn theo dõi CPU/RAM, quản lý người chơi và xem Console trực tiếp ngay trên trình duyệt.
- **Đồng bộ Đa Người Chơi (Multiplayer Sync):** Hỗ trợ nhiều người chơi cùng lúc. Bạn có thể nhìn thấy bạn bè của mình di chuyển, xoay góc nhìn, đập/đặt block và chat theo thời gian thực.
- **Tương tác cốt lõi:** Khởi tạo địa hình động, kênh chat toàn máy chủ, đập vỡ khối và đặt khối.

### 🚀 Hướng dẫn cài đặt

1. Đảm bảo máy tính của bạn đã cài đặt [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
2. Clone repository và mở thư mục dự án.
3. Biên dịch dự án:
   ```bash
   dotnet build -c Release
   ```
4. Khởi chạy máy chủ:
   ```bash
   dotnet run --project src/Aurora.Bootstrap -c Release
   ```

### 🎮 Hướng dẫn kết nối
- **Chơi một mình (Local):** Mở Minecraft 1.21.4, vào mục Multiplayer, thêm server với IP `localhost:25565` (hoặc `127.0.0.1`) và tham gia!
- **Chơi cùng bạn bè:** Máy chủ mặc định lắng nghe trên cổng `0.0.0.0:25565`, do đó bạn hoàn toàn có thể sử dụng các công cụ đường hầm (tunnel) như [playit.gg](https://playit.gg/) hoặc ngrok để mở server cho bạn bè vào chơi mà không cần mở port trên modem.
- **Web Dashboard:** Mở trình duyệt web và truy cập `http://localhost:5000` để xem Bảng điều khiển máy chủ.

### 🗺 Lộ trình phát triển
- [x] Hệ thống mạng cốt lõi và Handshake state.
- [x] Quy trình đăng nhập (Login) và Configuration state hiện đại (Bao gồm Đồng bộ Vanilla Registry NBT).
- [x] **Tương tác Cốt lõi (Core Mechanics):** Hệ thống tạo Chunk, hệ thống Chat, phá/đặt Block.
- [x] **Đa Người Chơi (Multiplayer Sync):** Quản lý trạng thái người chơi, khởi tạo Entity, và đồng bộ tọa độ.
- [ ] **Hệ thống Túi đồ (Inventory):** Quản lý Hotbar, tương tác vật phẩm và Item Metadata.
- [ ] **Khung Vật lý (Physics Engine):** Bounding box, trọng lực (gravity) và Entity Ticking.

### 🤝 Ghi nhận & Lời cảm ơn (Credits)
- **Tác giả:** icecnguyen
- **Trợ lý AI:** Phần lớn bộ khung ban đầu, quy trình gỡ lỗi và phân tích ngược giao thức mạng của dự án này được lập trình cặp (pair-programming) cùng **Antigravity (Google DeepMind)**.
- **Công cụ & Tài liệu:** 
  - Tham khảo giao thức mạng từ [wiki.vg](https://wiki.vg/) và [PrismarineJS/minecraft-data](https://github.com/PrismarineJS/minecraft-data).
  - Sử dụng `xUnit` để viết unit test kiểm tra các thuật toán.

Nếu bạn thấy dự án này thú vị, đừng quên tặng 1 ⭐ **Star** cho repository nhé!

---

## License
MIT License
