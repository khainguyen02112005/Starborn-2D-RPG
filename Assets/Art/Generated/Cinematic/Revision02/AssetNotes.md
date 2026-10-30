# Cinematic revision 02

Ảnh tạo bằng tính năng ImageGen tích hợp; PNG nền trong suốt được giữ alpha.
Không cần cài thêm Unity plugin để sử dụng các asset này.

- `HeroEmotions.png`: 32 sprite, 8 cột × 4 hàng. Hai hàng đầu: hiệp sĩ run sợ / quỳ khóc; hai hàng sau: pháp sư với cùng chuỗi động tác. Animator có Tremble, Despair và DespairHold.
- `CelebrationAndWarningVFX.png`: 48 sprite, 8 cột × 6 hàng. Pháo hoa vàng, cyan, hồng; quả cầu lửa; va chạm; vòng phép pixel tím.
- `DigitalCorruption.png`: ảnh nhiễu toàn màn hình. Material `DigitalCorruption.mat` tạo chuyển động xé dòng ở 12 frame/giây, dùng thời gian không phụ thuộc timeScale.

Animation Clips nằm trong `Animations`, Animator Controllers trong `Animators`, các prefab có thể kéo vào scene trong `Prefabs`. Các tham chiếu runtime nằm trong `Assets/Resources/CinematicGenerated/CinematicGeneratedLibrary.asset`.

Timeline vẫn nằm ở `Assets/Timelines/Cutscenes`. Marker OpeningStrike tự chờ nhân vật đến đúng tầm trước khi đánh; OpeningReturnToVillage đưa nhân vật về đúng điểm xuất phát. HeroTremble / HeroDespair dùng sprite mới; HeroOriginal trả về model SPUM. FadeInFromBlack thực hiện fade đầu cảnh.

## Prompts

### DigitalCorruption.png

Use case: stylized-concept. Asset type: opaque full-screen animated glitch texture for a 2D pixel RPG in Unity. Use attached photo only as supporting reference for the screen corruption pattern. Generate ONLY the abstract corrupted digital screen content filling the entire 16:9 image edge to edge. Absolutely no laptop, monitor, bezel, furniture, watermark, brand or readable words. Predominantly pure black with many irregular horizontal broken bands in electric magenta pink, white, cyan and sparse cobalt. Sharp tiny checkerboard and corrupted pixel-dither inside the bands, ragged discontinuous horizontal lengths like damaged video RAM. Reference the pink/cyan broken screen from the input. Flat orthographic texture, crisp hard-edged pixel art, no blur, no bloom, no gradients, no perspective. Balanced dense pattern across whole canvas, darkness at least 40 percent. Intended to tile and scroll in a Unity shader. Save the output image for use in a local Unity project.

### HeroEmotions.png

Use case: identity-preserve. Asset type: Unity 2D pixel sprite animation atlas, strictly 8 columns x 4 rows, 32 frames. References are the existing exact knight and mage SPUM sprites from the user's game. Keep their face proportions, tiny black eyes, red spiky hair and steel/brown armor/wood shield/sword of knight; mage has deep purple twin buns, plum robe, wooden purple-orb staff. Match EXACT low-resolution hard-edged SPUM pixel art, limited palette, NO smooth illustration or new design. Each equal-size cell square, centers aligned, all body parts inside cell with margin. Transparent background everywhere outside characters; no checkerboard printed, no labels, no borders, no shadows. Row1 eight consecutive frames KNIGHT trembling and raising free hand while looking upward in disbelief, subtle anticipation to recoil. Row2 eight consecutive KNIGHT lowering weapon, kneeling, head sinking into hands, crying silently in despair, weapon stays close beside body. Row3 eight consecutive frames MAGE trembling and recoiling in disbelief with same motion sequence as row1. Row4 eight consecutive MAGE sinking to knees, head lowered, crying silently with staff laid close beside. Fixed identical scale and feet baseline in every cell; same costume throughout. Draw all 32 separate frames completely, animation must follow smoothly left to right. No gore, no extra characters, no magical background effects. Output wide 2:1 atlas, prefer 2048x1024.

### CelebrationAndWarningVFX.png

Use case: stylized-concept. Asset type: Unity 2D pixel RPG VFX spritesheet 8 columns x 6 rows, 48 separate square equal-size frames, perfect regular grid, transparent alpha outside effects, no text labels borders checkerboard or background. Each row an eight-frame smooth animation left to right, centered consistently with 10% padding, never crosses another cell. Match low resolution SPUM game pixel art: crisp square pixels, limited colors, no soft airbrush or blur. Row1 golden celebration firework: narrow rocket rise, bright small star explosion, large branching radial golden spark bloom, then falling separated embers fading out. Row2 same firework in cyan/white. Row3 same firework in rose/purple. Row4 dark orange warning fireball building from ember to flaming sphere, irregular black-pixel corruption in flames, spherical fire core and short trailing tail, animation subtly rotates flame. Row5 orange red impact ring with cracked glass-like sharp luminous wedges, expands then dissolves to tiny embers, no square placeholder. Row6 subtle purple corruption gathering around a character: low magic circle, fragmented rising pixels and dark violet wisps, expands then dissolves, center stays clear. Output 4:3 landscape 2048x1536 if possible, all 48 frames individually drawn. Effects only, no humans, no props, no scenery, no gore.
