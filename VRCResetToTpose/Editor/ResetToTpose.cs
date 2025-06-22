using UnityEditor;
using UnityEngine;
using System; // System.Enum.GetValues のために必要

public class PoseResetter
{
    private const string MENU_PATH = "Tools/VRChat/Reset Avatar to Original Pose";

    [MenuItem(MENU_PATH)]
    private static void ResetSelectedAvatarToOriginalPose()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an avatar in the Hierarchy.", "OK");
            return;
        }

        Animator animator = selectedObject.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            EditorUtility.DisplayDialog("Error", "The selected object does not have an Animator with a Humanoid avatar.", "OK");
            return;
        }

        // 1. ソースとなるPrefabアセットのパスを取得
        string sourceAssetPath = AssetDatabase.GetAssetPath(animator.avatar);
        if (string.IsNullOrEmpty(sourceAssetPath))
        {
            // UnpackされているPrefabだとAvatarから直接パスが取れない場合がある
            sourceAssetPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(selectedObject));
            if (string.IsNullOrEmpty(sourceAssetPath))
            {
                EditorUtility.DisplayDialog("Error", "Could not find the source asset of the avatar. It might be a completely unpacked Prefab.", "OK");
                return;
            }
        }

        // 2. ソースアセット(FBXなど)を読み込む
        GameObject sourceObject = AssetDatabase.LoadAssetAtPath<GameObject>(sourceAssetPath);
        if (sourceObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to load the source asset.", "OK");
            return;
        }

        Animator sourceAnimator = sourceObject.GetComponent<Animator>();
        if (sourceAnimator == null)
        {
            EditorUtility.DisplayDialog("Error", "The source asset does not have an Animator.", "OK");
            return;
        }

        // Undo(元に戻す)操作を登録
        Undo.RecordObjects(animator.transform.GetComponentsInChildren<Transform>(), "Reset to Original Pose");

        // 3. すべてのHumanBodyBonesをループして、ソースのTransformに合わせる
        foreach (HumanBodyBones boneType in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (boneType == HumanBodyBones.LastBone) continue;

            Transform targetBone = animator.GetBoneTransform(boneType);
            Transform sourceBone = sourceAnimator.GetBoneTransform(boneType);

            if (targetBone != null && sourceBone != null)
            {
                targetBone.localPosition = sourceBone.localPosition;
                targetBone.localRotation = sourceBone.localRotation;
            }
        }
        
        // ルートオブジェクト自体のTransformもソースに合わせる
        selectedObject.transform.localPosition = sourceObject.transform.localPosition;
        selectedObject.transform.localRotation = sourceObject.transform.localRotation;
        selectedObject.transform.localScale = sourceObject.transform.localScale;

        EditorUtility.DisplayDialog("Success", "The avatar has been reset to its original pose.", "OK");
    }

    // メニューアイテムの有効/無効を判定するバリデーションメソッド
    [MenuItem(MENU_PATH, true)]
    private static bool ValidateResetToOriginalPose()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null) return false;

        Animator animator = selectedObject.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return false;
        
        // Prefabのソースが見つかるかどうかもチェック
        string path = AssetDatabase.GetAssetPath(animator.avatar);
        if (string.IsNullOrEmpty(path))
        {
            path = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(selectedObject));
        }

        return !string.IsNullOrEmpty(path);
    }
}