# AuroraDotNet
[English](#english) | [Tiếng Việt](#tiếng-việt)

---

<a name="english"></a>
## English

**AuroraDotNet** is an independent Minecraft server built from scratch in C# using .NET 9. It supports vanilla **Minecraft 1.21.4 (Protocol 768)** clients without requiring any mods.

The goal of this project is simple: create a fast, clean, and reliable Minecraft server that makes good use of modern .NET capabilities (like `System.IO.Pipelines`, `Span<T>`, and native multi-threading) while keeping game mechanics and world generation as close to the original game as possible.

### What works right now
- **Connecting & Playing**: You can join with an unmodified Minecraft 1.21.4 client. Handshake, configuration sync, and login are all fully working.
- **World Generation**: Multi-noise terrain generation inspired by vanilla and ported in part from [Pumpkin-MC](https://github.com/Pumpkin-MC/Pumpkin):
  - 10+ biomes (Plains, Forests, Birch Forests, Taiga, Deserts, Beaches, Snowy Mountains, and Oceans).
  - Proper height variation: ocean trenches, flat beaches, rolling hills, and mountain peaks.
  - Accurate ocean floor with sand, gravel, clay patches, seagrass, and kelp.
  - Realistic tree placement: trees only grow on valid soil (never on sand or in water), and leaves connect cleanly across chunk borders.
  - Natural structures: Ruined Nether Portals, Village houses (with foundation that adapts to slopes so houses don't float), Desert Wells, and Campsites.
- **3D Lighting**: Sunlight diffuses naturally through leaves and caves instead of creating pitch-black shadows. Torches, lanterns, campfires, and magma emit light properly.
- **Performance**: Chunks generate in parallel across all CPU cores, using memory pooling (`ArrayPool`) to prevent stutter and GC pauses while flying or exploring.
- **Multiplayer**: You can see other players move and look around, break and place blocks, and chat in real-time.
- **Web Dashboard**: An optional web interface at `http://localhost:5000` to monitor CPU/RAM usage and view live console logs.

### Quick Start

Prerequisites: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

```bash
# Clone the repository
git clone https://github.com/icecnguyen/AuroraDotNet.git
cd AuroraDotNet

# Run the server
dotnet run --project src/Aurora.Bootstrap -c Release
```

Once running, launch Minecraft 1.21.4, click **Multiplayer** -> **Direct Connection**, and enter `localhost:25565`.

If you want to play with friends over the internet without port-forwarding, you can tunnel port `25565` with tools like [playit.gg](https://playit.gg/) or ngrok.

### Next Steps
- [ ] Inventory and container interactions (chests, crafting tables, hotbar sync)
- [ ] Basic mob spawning and entity ticking
- [ ] Survival physics (falling, collisions, swimming)

### Credits & Acknowledgements
- **Author**: `icecnguyen`
- **Pumpkin-MC**: Special thanks to the team behind [Pumpkin-MC](https://github.com/Pumpkin-MC/Pumpkin) (an open-source Minecraft server in Rust). Key parts of our world generation architecture, noise climate sampling, and surface rules were ported and inspired by their work.
- **Protocol & Community Resources**:
  - [wiki.vg](https://wiki.vg/) for detailed packet and protocol specifications.
  - [PrismarineJS/minecraft-data](https://github.com/PrismarineJS/minecraft-data) for registry data and block state mappings.
- **AI Pair-Programming**: Developed and optimized with assistance from **Antigravity (Google DeepMind)**.
- **Disclaimer**: *Minecraft* is a trademark of Mojang Synergies AB. This project is an independent server implementation and is not affiliated with or endorsed by Mojang or Microsoft.

### License
Distributed under the [GNU General Public License v2.0 (GPL-2.0)](LICENSE).

---

<a name="tiếng-việt"></a>
## Tiếng Việt

**AuroraDotNet** là một server Minecraft độc lập được viết lại từ đầu bằng C# trên nền tảng .NET 9. Server hỗ trợ kết nối trực tiếp từ client **Minecraft Java 1.21.4 (Protocol 768)** nguyên bản mà không cần cài thêm bất kỳ bản mod nào.

Mục tiêu của dự án là xây dựng một server gọn gàng, mượt mà, tận dụng tối đa sức mạnh của .NET hiện đại (`System.IO.Pipelines`, `Span<T>`, xử lý đa luồng) nhưng vẫn giữ được cơ chế thế giới và trải nghiệm giống với Minecraft gốc nhất có thể.

### Những tính năng đã hoạt động
- **Vào game & kết nối**: Đăng nhập mượt mà bằng client 1.21.4 gốc. Hỗ trợ đầy đủ các bước handshake, configuration sync và play packet.
- **Tạo thế giới tự nhiên (World Gen)**: Sinh địa hình theo cơ chế Multi-Noise tương tự bản gốc và được port một phần từ dự án [Pumpkin-MC](https://github.com/Pumpkin-MC/Pumpkin):
  - Đầy đủ hơn 10 quần xã sinh vật (Đồng bằng, Rừng sồi, Rừng bạch dương, Rừng thông Taiga, Sa mạc, Bãi biển, Núi tuyết và Đại dương).
  - Độ cao địa hình rõ rệt: rãnh biển sâu, bãi biển bằng phẳng, đồi thoai thoải và núi cao chọc trời.
  - Đáy biển sinh động với thềm cát, sỏi, vỉa đất sét, cỏ biển và rặng tảo bẹ.
  - Cây cối mọc hợp lý: chỉ mọc trên đất hoặc cỏ (không mọc trên cát biển hay dưới nước), tán lá giữa các chunk liền mạch và không bị lá lơ lửng.
  - Công trình ngẫu nhiên: Cổng Nether đổ nát, Nhà dân làng (móng tự nối xuống đất nên không bị bay lơ lửng trên đồi dốc), Giếng sa mạc và Lều trại thám hiểm.
- **Ánh sáng 3D**: Ánh sáng mặt trời lan tỏa tự nhiên qua tán cây và vách đá, không còn bị bóng đen đặc dị thường. Đuốc, đèn lồng, lửa trại phát sáng ấm áp xung quanh.
- **Tối ưu tốc độ**: Sử dụng bộ nhớ tái dụng (`ArrayPool`) và đa luồng CPU (`Parallel.ForEach`) để tạo chunk nhanh, không bị khựng giật (GC lag) khi người chơi bay nhanh khám phá bản đồ.
- **Chơi nhiều người (Multiplayer)**: Nhìn thấy nhau chạy nhảy, quay góc nhìn, đập/đặt block và chat trực tiếp trong game.
- **Bảng điều khiển Web**: Giao diện web tại `http://localhost:5000` giúp theo dõi CPU, RAM, người chơi và xem log console trực tiếp.

### Cách chạy server

Yêu cầu: Máy đã cài [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

```bash
# Tải mã nguồn về máy
git clone https://github.com/icecnguyen/AuroraDotNet.git
cd AuroraDotNet

# Chạy server
dotnet run --project src/Aurora.Bootstrap -c Release
```

Sau khi server khởi động xong, mở Minecraft 1.21.4, chọn **Chơi mạng (Multiplayer)** -> **Kết nối trực tiếp (Direct Connection)** và nhập `localhost:25565`.

Để rủ bạn bè vào chơi chung mà không cần mở port modem, bạn có thể dùng các tool tunnel tiện lợi như [playit.gg](https://playit.gg/) hoặc ngrok trỏ vào port `25565`.

### Dự định tiếp theo
- [ ] Đồng bộ túi đồ và tương tác rương, bàn chế tạo
- [ ] Sinh quái vật/động vật và AI cơ bản
- [ ] Cơ chế va chạm vật lý, trọng lực và bơi lội

### Ghi nhận & Lời cảm ơn (Credits)
- **Tác giả / Người phát triển**: `icecnguyen`
- **Dự án Pumpkin-MC**: Xin gửi lời cảm ơn đặc biệt đến đội ngũ phát triển [Pumpkin-MC](https://github.com/Pumpkin-MC/Pumpkin) (server Minecraft mã nguồn mở viết bằng Rust). Một phần quan trọng trong kiến trúc tạo địa hình (world generation), ma trận khí hậu multi-noise và luật phủ bề mặt đã được học hỏi và port từ dự án này.
- **Tài liệu & Dữ liệu cộng đồng**:
  - [wiki.vg](https://wiki.vg/) với kho tài liệu chi tiết về đặc tả giao thức mạng Minecraft.
  - [PrismarineJS/minecraft-data](https://github.com/PrismarineJS/minecraft-data) cung cấp các báo cáo registry và bảng block state ID chuẩn xác.
- **Lập trình cặp cùng AI**: Dự án được xây dựng, tối ưu hóa thuật toán và gỡ lỗi cùng trợ lý lập trình **Antigravity (Google DeepMind)**.
- **Tuyên bố miễn trừ**: *Minecraft* là thương hiệu đã đăng ký của Mojang Synergies AB. Dự án này là phần mềm máy chủ độc lập, không liên kết hay được bảo trợ bởi Mojang hoặc Microsoft.

### Giấy phép (License)
Dự án được phát hành theo giấy phép [GNU General Public License v2.0 (GPL-2.0)](LICENSE).

