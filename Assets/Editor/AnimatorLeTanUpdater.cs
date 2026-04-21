using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public class AnimatorLeTanUpdater
{
    static AnimatorLeTanUpdater()
    {
        EditorApplication.delayCall += UpdateAnimator;
    }

    static void UpdateAnimator()
    {
        string path = "Assets/Scenes/AnimationGame/Receptionist 1/AniLeTan.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) return;

        // Nếu file controller quá rỗng, add Base Layer vào
        if (controller.layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
        }

        bool hasServingParam = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "isServing") hasServingParam = true;
        }

        if (hasServingParam) return; // Đã chạy xong, tránh chạy lại nhiều lần

        // Thêm tham số isServing
        controller.AddParameter("isServing", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Load 3 Animation Clips
        AnimationClip idleClip = LoadClip("Assets/Scenes/AnimationGame/Receptionist 1/Female_Civilian@Happy Idle.fbx");
        AnimationClip bowClip = LoadClip("Assets/Scenes/AnimationGame/Receptionist 1/Female_Civilian@Quick Formal Bow.fbx");
        AnimationClip talkingClip = LoadClip("Assets/Scenes/AnimationGame/Receptionist 1/Female_Civilian@Talking.fbx");

        if (idleClip == null || bowClip == null || talkingClip == null)
        {
            Debug.LogError("[AniLeTan] Lỗi: Không tìm thấy Animation fbx cho Lễ Tân!");
            return;
        }

        // Tạo 3 States
        AnimatorState idleState = rootStateMachine.AddState("Happy Idle");
        idleState.motion = idleClip;

        AnimatorState bowState = rootStateMachine.AddState("Quick Formal Bow");
        bowState.motion = bowClip;

        AnimatorState talkingState = rootStateMachine.AddState("Talking");
        talkingState.motion = talkingClip;

        // Set State mặc định
        rootStateMachine.defaultState = idleState;

        // Nối dây: Idle -> Bow (khi có khách)
        var idleToBow = idleState.AddTransition(bowState);
        idleToBow.AddCondition(AnimatorConditionMode.If, 0, "isServing");
        idleToBow.hasExitTime = false; // Phản ứng ngay
        idleToBow.duration = 0.25f;

        // Nối dây: Bow -> Talking (tự động chuyển sau khi cúi chào xong)
        var bowToTalking = bowState.AddTransition(talkingState);
        bowToTalking.hasExitTime = true;
        bowToTalking.exitTime = 0.85f; // Đợi sắp cúi xong thì múa tay nói chuyện
        bowToTalking.duration = 0.25f;

        // Nối dây: Talking -> Idle (khách đi khỏi, ngưng phục vụ)
        var talkingToIdle = talkingState.AddTransition(idleState);
        talkingToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isServing");
        talkingToIdle.hasExitTime = true; 
        talkingToIdle.exitTime = 0.8f; // Đợi hành động múa tay nói chuyện tròn trịa rồi mới nghỉ
        talkingToIdle.duration = 0.5f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("[AniLeTan] Nối dây thành công: Idle -> Bow -> Talking!");
    }

    static AnimationClip LoadClip(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip && !asset.name.StartsWith("__preview__"))
            {
                return asset as AnimationClip;
            }
        }
        return null;
    }
}
