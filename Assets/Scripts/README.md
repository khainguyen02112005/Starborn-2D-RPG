# StarBorn — nhóm mã nguồn

Giữ tên lớp và GUID khi di chuyển file để scene/prefab không mất liên kết.

- Core: lưu/khôi phục, tiến trình từng scene, tên người chơi và danh mục vật phẩm.
- Player: điều khiển, lớp hiệp sĩ/pháp sư, chỉ số và hoạt ảnh SPUM.
- Enemies: quái, boss, hoạt ảnh và điểm hồi sinh.
- Combat: đòn đánh, kỹ năng, đạn, hiệu ứng và trận con trỏ kết 2.
- Inventory: túi đồ, trang bị, sách, rương, thương nhân và lò rèn.
- World: bản đồ, vật lý/độ sâu, cổng, NPC và nhiệm vụ.
- Cinematics: Timeline, cốt truyện, các kết, nhiễu và cuộn phim quay lại.
- UI: menu, HUD, thành tựu, bản đồ nhỏ, loading và hướng dẫn phím.
- Audio: nguồn âm thanh, thư viện, nút âm lượng và âm UI.
- Editor: công cụ Inspector, không đưa vào bản build.

## Chỉnh bằng Unity Editor

- Hồi sinh: chọn `Respawn_*` trong Level_02/Level_03, chỉnh `Respawn Delay` (hiện là 7 giây). Boss không có điểm hồi sinh.
- Rương: chọn `OutpostChest_*` trong Level_03; `Initial Items/Amounts` là đồ khởi đầu, mỗi rương có trang sách và dữ liệu lưu riêng. `SolidChestFoot` chỉ chặn phần chân; collider trigger lớn dùng để nhấn E.
- Thanh máu/nhấp đỏ kết 2: `Assets/Prefabs/Resources/GameUI/Ending2/BattleHUD.prefab`, animation trong `Assets/Animations/Ending2`.
- Âm thanh: `Assets/Prefabs/Resources/AudioRig.prefab`; các clip/gain trong `Assets/Resources/GameAudioLibrary.asset`.
- Cổng: chọn `Portal_To_*` trong scene. Mặt bệ là nền đi được; không bật lại collider toàn bộ hình bệ.
- Cutscene: các asset trong `Assets/Timelines/Cutscenes`. Tốc độ phát tăng nhẹ 1.12 lần; thời gian đọc vẫn phụ thuộc độ dài thoại.

Không thay toàn bộ logic lưu, chiến đấu hay các nhánh kết bằng hệ thống khác: những phần này cần C# để giữ chính xác hành vi đang chạy. Các bố cục/nguồn âm/hoạt ảnh cố định được lưu thành asset Unity để chỉnh trực tiếp.

Script cũ `GameManager.cs` không còn tham chiếu đã được gỡ; bản phục hồi cả script và meta ở `Backups/FullPolish_20261019/Original/Assets/Scripts`.
