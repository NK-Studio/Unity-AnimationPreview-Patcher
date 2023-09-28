using NKStudio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AnimationPatcher : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset _visualTreeAsset;

    [MenuItem("Window/Animation Patcher")]
    public static void ShowExample()
    {
        AnimationPatcher wnd = GetWindow<AnimationPatcher>();
        wnd.titleContent = new GUIContent("Animation Patcher");
        wnd.minSize = new Vector2(434, 140);
        wnd.maxSize = new Vector2(434, 140);
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        
        VisualElement visualTree = _visualTreeAsset.Instantiate();
        root.Add(visualTree);

        bool isActive = APCore.IsInstalled();
        Button installButton = root.Q<Button>("patch-button");
        Label state = root.Q<Label>("state-text");
        
        installButton.text = isActive ? "Unpatch" : "Patch";
        string onoff = isActive ? "On" : "Off";
        state.text = $"활성화 상태 : {onoff}";
        
        installButton.clicked += () =>
        {
            if (isActive)
                APCore.UnInstall();
            else
                APCore.Install();
        };
    }
}
