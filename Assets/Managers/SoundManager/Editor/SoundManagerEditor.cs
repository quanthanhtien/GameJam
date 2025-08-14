using System;
using UnityEditor;
using UnityEngine;

// ReSharper disable CheckNamespace

[CustomEditor(typeof(SoundManager))] 
public class SoundManagerEditor : Editor
{
    //
    private SoundManager _soundManager;
    private SerializedObject _sManager;

    //
    private void OnEnable()
    {
        _soundManager = (SoundManager)target;
        _sManager = new SerializedObject(target);
    }

    //
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var droppedObject = DropAreaGUI();
        if (droppedObject.Length != 0)
        {
            foreach (var droppedObj in droppedObject)
            {
                var splitObj = droppedObj.ToString().Split('/');
                var nameObj = splitObj[splitObj.Length - 1];
                var splitSoundName = nameObj.Split('-');
                if (splitSoundName.Length == 0 || splitSoundName.Length > 2)
                {
                    Debug.LogWarning("Check name of AudioClip.");
                    continue;
                }

                var soundName = splitSoundName[0];
                var soundClip = droppedObj as AudioClip;
                var soundGroup = TrackHelper.Background;
                var soundVolume = 1f;
                var soundPitch = 1f;
                var soundLoop = false;
                var soundFadeIn = false;
                var soundFadeInTime = 0f;
                var soundFadeOut = false;
                var soundFadeOutTime = 0f;

                if (splitSoundName[1].Contains("Bgm"))
                {
                    soundGroup = TrackHelper.Background;
                    soundLoop = true;
                    soundFadeIn = true;
                    soundFadeInTime = .25f;
                    soundFadeOut = true;
                    soundFadeOutTime = .25f;
                }
                else if (splitSoundName[1].Contains("Eft"))
                {
                    soundGroup = TrackHelper.Effect;
                }
                else if (splitSoundName[1].Contains("Ui"))
                {
                    soundGroup = TrackHelper.UInterface;
                }
                else
                {
                    Debug.LogWarning("Check name of AudioClip, set to background group.");
                }

                var myNewSound = new SoundConfig(soundName, soundClip, soundGroup, soundVolume, soundPitch, soundLoop, soundFadeIn, soundFadeInTime, soundFadeOut, soundFadeOutTime);
                _soundManager.ConfigSounds.Add(myNewSound);
            }
        }

        _sManager.ApplyModifiedProperties();

        EditorUtility.SetDirty(target);
    }

    //
    public object[] DropAreaGUI()
    {
        var toReturn = Array.Empty<object>();

        var evt = Event.current;
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        var drop_area = GUILayoutUtility.GetRect(350.0f, 70f, GUILayout.ExpandWidth(true));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUI.Box(drop_area, "Drag multiple AudioClips here.\nBackground clips end with -Bgm.\nEffect clips end with -Eft.\nUInterface clips end with -Ui.");

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (drop_area.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        var canGo = false;
                        var quantityToGo = 0;
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is AudioClip)
                            {
                                canGo = true;
                                quantityToGo++;
                            }
                        }

                        if (canGo)
                        {
                            DragAndDrop.AcceptDrag();
                            toReturn = new object[quantityToGo];
                            var counter = 0;
                            for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                            {
                                if (DragAndDrop.objectReferences[i] is AudioClip)
                                {
                                    DragAndDrop.objectReferences[i].name = DragAndDrop.paths[i];
                                    toReturn[counter] = DragAndDrop.objectReferences[i];
                                    counter++;
                                }
                            }
                        }
                    }
                }

                break;
        }

        return toReturn;
    }
}