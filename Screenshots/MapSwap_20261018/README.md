# Ghép map — 18/10/2026

## Bản đồ đang dùng

| Scene chơi | Bản đồ lấy từ gói |
| --- | --- |
| Level_02 | Level_11 — làng |
| Level_03 | Level_12 — rừng |
| Level_04 | Level_13 — phòng boss |

Boss chỉ còn ở Level_04. Đường đi nối hai chiều Level_02 ↔ Level_03 ↔ Level_04. Các cổng vẫn dùng E và cơ chế LoadingScene hiện có; điểm tới được bố trí gần lối tương ứng.

Không import script điều khiển, UI, nhân vật hoặc chức năng từ gói. Chỉ đưa vào tài nguyên cần cho map; bỏ các hành vi demo khỏi phần địa hình. Nhân vật SPUM, trang bị, kỹ năng, NPC, thương nhân, thợ rèn, rương, UI và cutscene của project hiện tại được giữ lại.

## Chỉnh map trong Unity

- Hierarchy của từng scene: `Map_From_Level_11`, `Map_From_Level_12`, `Map_From_Level_13`.
- Level_02: tường thật dùng collider ở `Grid/Obstacles` và `Grid/ha`. Sàn, đường, bậc thang và phần trang trí không dùng collider phủ kín. Va chạm cây/nhà/đồ lớn đặt ở chân vật thể; tán cây và mái được sắp lớp theo vị trí chân.
- Đồ trang trí có Rigidbody động được đổi sang Static, tắt trọng lực. Không đổi Rigidbody/AI của nhân vật hay quái đang chơi.
- `MapSceneLayout` trên gốc map chứa biên đi được, sàn, điểm xuất hiện và các điểm tới của cổng. `MapDepthSort` trên nhân vật/quái/NPC giữ thứ tự trước–sau và các mảnh SPUM cùng một nhóm.
- Hồi sinh vẫn theo cơ chế gần vị trí an toàn gần nhất, không bị ép trở lại đầu map; vị trí được kiểm tra tránh tường trước khi hồi sinh.

## Kiểm tra đã chạy

Xem `Playtest.txt` và `Backups/MapSwap_20261018/EditAudit.txt`.

- Cả hiệp sĩ và pháp sư: di chuyển bằng Rigidbody qua map, đánh thường trúng quái, dùng lửa/nước/sét và hiển thị VFX/sát thương.
- Đi đến NPC, rương, thợ rèn, thương nhân; lên đúng bậc thang sân cao của map làng. Túi đồ và mua/dùng bình mana vẫn hoạt động.
- Qua cổng đi/về 02 → 03 → 04 → 03 → 02, giữ vàng, mana, kỹ năng và điểm xuất hiện hợp lệ.
- Boss chờ ở phòng mới, chỉ vào cảnh gặp boss khi tới gần; đối thoại hoàn tất mới đánh. Cảnh đánh bại boss và cập nhật nhiệm vụ vẫn chạy.
- Snapshot Level_04 giữ HP/trạng thái boss. Save cũ được xử lý khi tọa độ không còn hợp lệ; trạng thái boss từ Level_03 cũ chuyển sang Level_04; quái đã hạ và đồ trong rương vẫn giữ được.
- Kiểm tra nhân vật đứng trước/sau tán cây, vật trang trí không tự di chuyển, Game Over và hồi sinh đầy HP/mana trên cả ba map.
- Đối chiếu dữ liệu lưu thật sau thử nghiệm: không thay đổi các khóa đã sao lưu hoặc năm ô lưu. Editor được để ở Main menu, tắt Play Mode.

Ảnh toàn map: `Backups/MapSwap_20261018/IntegratedLevel02.png`, `IntegratedLevel03.png`, `IntegratedLevel04.png`. Ảnh gameplay nằm cùng thư mục README này.

## Sao lưu và phạm vi

Bản trước khi ghép ở `Backups/MapSwap_20261018/Original`; dữ liệu lưu gốc ở `PlayerPrefs.json`. Gói `D:/src unity/hahaha.unitypackage` và TagManager gốc không bị sửa. Các scene nhập trung gian đã được dọn khỏi Assets sau khi sao chép map; vẫn khôi phục được từ gói và bản giải nén trong backup. Tài nguyên map cần thiết nằm ở `Assets/ImportedMaps/Dependencies`.

Không cài plugin, không thêm menu Tools. Các file dùng để kiểm tra/xây dựng hàng loạt được giữ ngoài Assets nên không trở thành công cụ Editor hay script gameplay trong project.
