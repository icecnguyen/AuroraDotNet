# AuroraDotNet
[English](#english) | [Tiếng Việt](#tiếng-việt)

---

<a name="english"></a>
## 🇬🇧 English

### Overview
**AuroraDotNet** is an experimental, high-performance Minecraft server implementation written entirely in C# (.NET 9.0). Designed from the ground up to support the modern Minecraft 1.21.4 (Protocol 768) specification, this project aims to explore the capabilities of zero-allocation networking and high-throughput memory management in C#.

### Technical Highlights
- **Protocol 768 (Minecraft 1.21.4):** Adheres to the latest network protocols including the strict `Configuration` state handshake (Feature Flags, Known Packs, Registry Data).
- **High-Performance I/O:** Built on top of `System.IO.Pipelines`, `ReadOnlySequence`, and `Span<T>` to ensure extremely fast packet parsing with virtually zero heap allocations.
- **Modular Architecture:** The codebase is separated into distinct, maintainable modules:
  - `Aurora.Network`: Asynchronous socket management and pipeline streaming.
  - `Aurora.Protocol`: Raw data parsing, packet serialization, and state machines.
  - `Aurora.Core`: Primitive data structures, mathematical operations, and global concepts.
  - `Aurora.World`: Chunk management, coordinate conversions, and terrain structures.

### Getting Started

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

### Roadmap
- [x] Basic networking and Handshake state.
- [x] Login and modern Configuration state flow.
- [ ] **Vanilla Registry NBT Sync:** The server requires a proper `minecraft:dimension_type` and `minecraft:worldgen/biome` NBT dump from a vanilla 1.21.4 server to successfully bypass the client's strict validation.
- [ ] World & Chunk Generation (Anvil format).
- [ ] Entity, physics engine, and bounding box validation.

### Credits & Acknowledgements
- **Author/Creator:** icecnguyen
- **AI Assistance:** A significant portion of this project's initial scaffolding, debugging, and protocol reverse-engineering was pair-programmed alongside **Antigravity (Google DeepMind)**.
- **Tools & Libraries:** 
  - Protocol references heavily sourced from [wiki.vg](https://wiki.vg/) and [PrismarineJS/minecraft-data](https://github.com/PrismarineJS/minecraft-data).
  - Uses `xUnit` for comprehensive bitwise and spatial mathematical testing.

### Contributions & Recommendations
If you found this project helpful or interesting, consider supporting the development:
- ⭐ **Star this repository** to help it reach more developers!
- 🤝 **Contributions:** Feel free to open issues or submit pull requests.
- 💡 **Recommendations:** I highly recommend checking out [PrismarineJS](https://github.com/PrismarineJS) for their excellent protocol documentation and tools, which heavily inspired this project.

---

<a name="tiếng-việt"></a>
## 🇻🇳 Tiếng Việt

### Tổng quan
**AuroraDotNet** là một dự án máy chủ Minecraft thử nghiệm với hiệu suất cao, được viết hoàn toàn bằng ngôn ngữ C# (trên nền tảng .NET 9.0). Được thiết kế để hỗ trợ đặc tả giao thức của Minecraft 1.21.4 (Protocol 768), dự án này tập trung vào việc khai phá sức mạnh của các kỹ thuật quản lý bộ nhớ không cấp phát (zero-allocation) và băng thông mạng cực cao trong C#.

### Điểm nổi bật về kỹ thuật
- **Giao thức 768 (Minecraft 1.21.4):** Tuân thủ chặt chẽ các chuẩn mạng mới nhất, bao gồm cả quá trình handshake khắt khe của state `Configuration` (Feature Flags, Known Packs, Registry Data).
- **I/O Hiệu suất cao:** Xây dựng dựa trên `System.IO.Pipelines`, `ReadOnlySequence`, và `Span<T>` để đảm bảo tốc độ phân tích packet cực nhanh và hầu như không xả rác bộ nhớ (zero heap allocations).
- **Kiến trúc Module:** Mã nguồn được phân chia thành các thư viện riêng biệt, dễ bảo trì:
  - `Aurora.Network`: Quản lý Socket bất đồng bộ và luồng dữ liệu pipeline.
  - `Aurora.Protocol`: Xử lý phân tích dữ liệu thô, mã hóa/giải mã packet và State Machine.
  - `Aurora.Core`: Các cấu trúc dữ liệu nguyên thủy, tính toán toán học và khái niệm toàn cục.
  - `Aurora.World`: Quản lý Chunk, chuyển đổi hệ tọa độ không gian và cấu trúc địa hình.

### Hướng dẫn cài đặt

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

### Lộ trình phát triển
- [x] Hệ thống mạng cốt lõi và Handshake state.
- [x] Quy trình đăng nhập (Login) và Configuration state hiện đại.
- [ ] **Đồng bộ Vanilla Registry NBT:** Để client có thể chính thức chuyển sang Play state, dự án cần nạp file dump NBT của `minecraft:dimension_type` và `minecraft:worldgen/biome` từ server Vanilla 1.21.4.
- [ ] Hệ thống tạo Chunk và Thế giới (chuẩn Anvil).
- [ ] Khung vật lý, Entity và kiểm tra va chạm (bounding box).

### Ghi nhận & Lời cảm ơn (Credits)
- **Tác giả:** icecnguyen
- **Trợ lý AI:** Phần lớn bộ khung ban đầu, quy trình gỡ lỗi và phân tích ngược giao thức mạng của dự án này được lập trình cặp (pair-programming) cùng **Antigravity (Google DeepMind)**.
- **Công cụ & Tài liệu:** 
  - Tham khảo giao thức mạng từ [wiki.vg](https://wiki.vg/) và [PrismarineJS/minecraft-data](https://github.com/PrismarineJS/minecraft-data).
  - Sử dụng `xUnit` để viết unit test kiểm tra các thuật toán dịch bit và không gian 3D.

### Đóng góp & Đề xuất
Nếu bạn thấy dự án này thú vị hoặc có ích, hãy cân nhắc ủng hộ nhé:
- ⭐ **Tặng 1 sao (Star) cho repository này** để giúp dự án được nhiều người biết đến hơn!
- 🤝 **Đóng góp:** Chào đón mọi Pull Request và Issue đóng góp từ cộng đồng.
- 💡 **Đề xuất (Recommendations):** Nếu bạn muốn tìm hiểu sâu về lập trình server Minecraft, tôi thực sự khuyên bạn nên tham khảo [PrismarineJS](https://github.com/PrismarineJS) – bộ tài liệu giao thức và công cụ của họ cực kỳ tuyệt vời và là nguồn cảm hứng lớn cho dự án này.

---

## License
MIT License
