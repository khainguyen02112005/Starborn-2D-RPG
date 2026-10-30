#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Level04Controller))]
public class Level04ControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Level04Controller controller = (Level04Controller)target;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("🎬 TRÌNH CHIẾU CUTSCENE TRỰC TIẾP", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("⚠️ BẠN CẦN BẤM NÚT PLAY ▶ (TRÊN ĐỈNH MÀN HÌNH UNITY) TRƯỚC THÌ MỚI XEM ĐƯỢC CUTSCENE!", MessageType.Warning);
            
            GUI.backgroundColor = new Color(0.25f, 0.75f, 0.35f);
            if (GUILayout.Button("▶ BẤM VÀO ĐÂY ĐỂ BẬT PLAY MODE NGAY", GUILayout.Height(36)))
            {
                EditorApplication.isPlaying = true;
            }
            GUI.backgroundColor = Color.white;
            return;
        }

        EditorGUILayout.HelpBox("Đang trong Play Mode! Bấm các nút dưới đây hoặc bấm phím tắt F1-F6:", MessageType.Info);

        GUI.backgroundColor = new Color(0.35f, 0.75f, 1f);
        if (GUILayout.Button("▶ 1. Xem Cutscene Mở Đầu (Opening Village) [F1]", GUILayout.Height(34)))
        {
            controller.DebugPlayOpeningVillage();
        }

        GUI.backgroundColor = new Color(0.95f, 0.85f, 0.35f);
        if (GUILayout.Button("▶ 2. Xem Sứ Giả Hoàng Gia (Royal Decree) [F2]", GUILayout.Height(30)))
        {
            controller.DebugPlayRoyalDecree();
        }

        GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
        if (GUILayout.Button("▶ 3. Xem Kết 1 (Happy To Bad Ending) [F3]", GUILayout.Height(30)))
        {
            controller.DebugPlayEnding1();
        }

        GUI.backgroundColor = new Color(0.75f, 0.55f, 1f);
        if (GUILayout.Button("▶ 4. Xem Kết 2 (Hiệp Sĩ Gục Ngã - Reset Loop) [F4]", GUILayout.Height(30)))
        {
            controller.DebugPlayEnding2();
        }

        GUI.backgroundColor = new Color(0.92f, 0.92f, 0.95f);
        if (GUILayout.Button("▶ 5. Xem Kết 3 (Khoảng Trắng - White Void) [F5]", GUILayout.Height(30)))
        {
            controller.DebugPlayEnding3();
        }

        GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
        if (GUILayout.Button("▶ 6. Xem Vỡ Kính & Popup Lỗi (Glitch Pause) [F6]", GUILayout.Height(30)))
        {
            controller.DebugPlayGlitchPause();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif
