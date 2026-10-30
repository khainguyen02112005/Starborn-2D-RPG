# Kết 2 — chém con trỏ và choáng

Asset native tự tạo, không vẽ/generate ảnh ở mỗi frame:

- `FivePointStar.asset`: mesh sao 5 cánh cho hiệu ứng choáng.
- `SlashFlash.asset`: mesh tia lóe khi chém trúng.
- `SlashCyan.mat` / `StunGold.mat`: màu xanh-trắng và vàng theo hiệu ứng chiến đấu/UI của game.
- Prefab: `Assets/Prefabs/Resources/CombatVFX/Ending2` gồm SlashTrail, SlashImpact, StunStars.
- Animator/clip vòng sao: `Assets/Animations/Ending2/CursorSlash`.

Trên `CursorBattleRoot` trong Level_02: Slash Damage = 12, Slash Cooldown = 0,28 giây. Giữ chuột phải và kéo vệt xuyên qua đối thủ; giữ yên hoặc rê vòng nhỏ bên trong không gây sát thương lặp. Thả chuột phải không còn bắn đạn theo nét vẽ.

Trên thành phần `CorruptedKnightBoss` của PlayerRoot: dồn đủ 30 sát thương, với khoảng ngắt tối đa 1,6 giây giữa các đòn, gây choáng 0,9 giây. Sau khi hết choáng có 1,4 giây chống choáng lại. Các giá trị đều chỉnh được trong Inspector. Choáng ngắt đòn áp sát/dịch chuyển đang chuẩn bị nhưng không xóa phép đã bắn.

Vòng sao, thời gian choáng và xử lý sát thương dừng khi narrative-pause hoạt động. Chuột trái vẫn bắn thẳng như trước. Con trỏ chỉ có một mạng trong trận này, hết máu tiếp tục nhánh kết có sẵn.
