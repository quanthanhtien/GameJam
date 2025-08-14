using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace TinyGiantStudio.BetterInspector
{
    /// <summary>
    /// Methods containing the word Setup are called once when the inspector is created.
    /// Methods containing the word Update can be called multiple times to update to reflect changes.
    ///
    ///
    /// To-do:
    ///
    /// Consider :
    /// A different implementation of the reset material method for checkered texture
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MeshFilter))]
    public class BetterMeshFilterEditor : Editor
    {
        #region Variable Declarations

        /// <summary>
        /// If reference is lost, retrieved from file location
        /// </summary>
        [SerializeField]
        private VisualTreeAsset visualTreeAsset;

        private readonly string visualTreeAssetFileLocation = "Assets/Plugins/Tiny Giant Studio/Better Inspector/Better Mesh Filter/Scripts/Editor/BetterMeshFilter.uxml";
        private string learnMoreAboutRuntimeMemoryUsageLink = "https://ferdowsur.gitbook.io/better-mesh/full-feature-list/runtime-memory-size";

        private VisualElement root;

        private List<Mesh> meshes = new List<Mesh>();
        private MeshFilter sourceMeshFilter;//This will not be updated if multiple object is selected
        private Mesh mesh; //This will not be updated if multiple object is selected

        //Preview Containers
        private GroupBox meshPreviewContainerGroupBox;

        private GroupBox allSelectedMeshCombinedDetails;

        private MeshPreview[] meshPreview = new MeshPreview[4];
        private IMGUIContainer previewSettingsContainer;
        private ObjectField meshField;

        private Editor originalEditor;

        private BetterMeshSettings editorSettings;

        #region Editor Settings Variables

        private bool showMeshPreview = true;

        #region Information Inspector Settings Variables

        private bool showInformationFoldout = true;
        private GroupBox informationFoldout;

        private bool showVertexInformation = true;

        private bool showTriangleInformation = true;

        private bool showEdgeInformation = false;

        private bool showFaceInformation = true;

        private bool showTangentInformation = true;

        #endregion Information Inspector Settings Variables

        private bool showSizeFoldout = true;
        private GroupBox sizeFoldout;

        private Label assetLocationOutsideFoldout;
        private GroupBox RuntimeMemoryUsageOutsideFoldout;

        private bool showAssetLocationInFoldout = true;
        private GroupBox assetLocationInInformationFoldoutGroupBox;
        private Label assetLocationLabel;
        private GroupBox runtimeMemoryUsageInInformationFoldout;
        private Label memoryUsageInFoldout;

        private bool showDebugGizmoFoldout = true;
        private GroupBox gizmoFoldout;

        #region Actions Inspector Settings

        private bool showActionsFoldout = true;
        private GroupBox actionsFoldout;

        private bool showOptimizeButton = true;

        private bool showRecalculateNormalsButton = true;

        private bool showRecalculateTangentsButton = true;

        private bool showFlipNormalsButton = true;

        private GroupBox lightmapGenerateFoldout;
        private bool showGenerateSecondaryUVButton = true;
        private SliderInt lightmapGenerate_hardAngle;
        private SliderInt lightmapGenerate_angleError;
        private SliderInt lightmapGenerate_areaError;
        private SliderInt lightmapGenerate_packMargin;

        private bool showSaveMeshButtonAs = true;

        #endregion Actions Inspector Settings

        private bool doNotApplyActionToAsset = true;

        #endregion Editor Settings Variables

        /// <summary>
        /// This is used to prevent information needlessly being updated on change.
        /// </summary>
        [SerializeField] private bool updatedInformation = false;

        /// <summary>
        /// Updated by UpdateMeshField();
        /// </summary>
        private string assetPath = string.Empty;

        private CustomFoldoutSetup customFoldoutSetup;

        #endregion Variable Declarations

        #region Unity Stuff

        //This is unnecessary. Just being overly careful.
        private void OnDestroy()
        {
            CleanUpUnusedMemory();
        }

        private void OnDisable()
        {
            CleanUpUnusedMemory();
        }

        /// <summary>
        /// CreateInspectorGUI is called each time something else is selected with this one locked.
        /// </summary>
        /// <returns></returns>
        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();

            if (target == null)
                return root;

            //In-case reference to the asset is lost, retrieve it from file location
            if (visualTreeAsset == null) visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(visualTreeAssetFileLocation);

            //If can't find the Better Mesh UXML,
            //Show the default inspector
            if (visualTreeAsset == null)
            {
                LoadDefaultEditor();
                return root;
            }

            visualTreeAsset.CloneTree(root);

            GetEditorSettings();
            customFoldoutSetup = new CustomFoldoutSetup();

            SetupMeshField();
            SetupInformationFoldout();

            if (targets.Length == 1)
            {
                SetupSizeFoldout();
                SetupActionsFoldout();
                SetupMeshDebugFoldout();
                SetupInspectorSettingsFoldout();
            }
            else
            {
                root.Q<GroupBox>("MeshSize").style.display = DisplayStyle.None;
                root.Q<GroupBox>("Buttons").style.display = DisplayStyle.None;
                root.Q<GroupBox>("MeshDebug").style.display = DisplayStyle.None;

                SetupInspectorSettingsFoldout();
                //inspectorSettingsFoldout = root.Q<GroupBox>("InspectorSettings");
                //inspectorSettingsFoldout.style.display = DisplayStyle.None;
                //foldoutColorField = inspectorSettingsFoldout.Q<ColorField>("FoldoutColorField");
            }

            //This will update the inspector according to the selected settings
            UpdateInspector();

            UpdateInspectorColor();

            ColorField previewColor = root.Q<ColorField>("PreviewColorField");
            previewColor.value = editorSettings.PreviewBackgroundColor;
            previewColor.RegisterValueChangedCallback(ev =>
            {
                editorSettings.PreviewBackgroundColor = ev.newValue;
                UpdateMeshPreviews();
            });

            UpdateMeshFieldGroupPosition();

            return root;
        }

        private void UpdateMeshFieldGroupPosition()
        {
            GroupBox meshFieldGroupBox = root.Q<GroupBox>("MeshFieldGroupBox");
            if (editorSettings.meshFieldOnTop)
                root.Q<GroupBox>("RootHolder").Insert(0, meshFieldGroupBox);
            else
                root.Q<GroupBox>("MainContainer").Insert(0, meshFieldGroupBox);
        }

        #endregion Unity Stuff

        [SerializeField] private Material checkerMaterial = null;

        private void SetupMeshField()
        {
            meshField = root.Q<ObjectField>("mesh");
            assetLocationOutsideFoldout = root.Q<Label>("AssetLocationOutsideFoldout");
            RuntimeMemoryUsageOutsideFoldout = root.Q<GroupBox>("RuntimeMemoryUsageOutsideFoldout");
            meshPreviewContainerGroupBox = root.Q<GroupBox>("PreviewContainers");
            allSelectedMeshCombinedDetails = meshPreviewContainerGroupBox.Q<GroupBox>("AllSelectedMeshCombinedDetails");

            UpdateMeshesReferences();

            meshField.RegisterValueChangedCallback(ev =>
            {
                UpdateMeshesReferences();
                UpdateInspector();
                UpdateMeshTexts();
            });
            UpdateMeshTexts();
        }

        /// <summary>
        /// This updates the tool-tip and labels with asset location
        /// </summary>
        private void UpdateMeshTexts()
        {
            assetLocationOutsideFoldout.text = assetPath;
            meshField.tooltip = assetPath;
        }

        /// <summary>
        /// This is used at the beginning and when mesh is updated
        /// </summary>
        /// <param name="currentMesh"></param>
        private void UpdateInspector()
        {
            updatedInformation = false;

            if (meshes.Count > 0)
            {
                if (showMeshPreview)
                    UpdateMeshPreviews();
                else
                    HideMeshPreview();

                UpdateFoldouts();

                if (editorSettings.ShowAssetLocationBelowMesh && targets.Length == 1) assetLocationOutsideFoldout.style.display = DisplayStyle.Flex;
                else assetLocationOutsideFoldout.style.display = DisplayStyle.None;
            }
            else //no mesh
            {
                HideAllFoldouts();
                HideMeshPreview();
                assetLocationOutsideFoldout.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// This updates the following:
        /// List<Mesh> meshes;
        /// Mesh mesh;
        /// MeshFilter sourceMeshFilter;
        /// string assetPath;
        /// </summary>
        private void UpdateMeshesReferences()
        {
            meshes.Clear();

            if (targets.Length > 1)
            {
                foreach (MeshFilter meshFilter in targets.Cast<MeshFilter>())
                {
                    if (meshFilter.sharedMesh != null) meshes.Add(meshFilter.sharedMesh);
                }

                assetPath = string.Empty;
            }
            else
            {
                sourceMeshFilter = target as MeshFilter;
                mesh = sourceMeshFilter.sharedMesh;
                meshes.Clear();
                if (mesh != null)
                    meshes.Add(mesh);

                if (mesh == null) assetPath = "No mesh found.";
                else if (MeshIsAnAsset(mesh)) assetPath = AssetDatabase.GetAssetPath(mesh);
                else assetPath = "The mesh is not connected to an asset.";
            }
        }

        #region Preview

        private void UpdateMeshPreviews()
        {
            Button inspectorSettingsButton2 = root.Q<Button>("InspectorSettingsButton2");
            inspectorSettingsButton2.style.display = DisplayStyle.None;

            meshPreviewContainerGroupBox.style.display = DisplayStyle.Flex;
            GroupBox groupBox = meshPreviewContainerGroupBox.Q<GroupBox>("PreviewsGroupBox");

            int i = 0;
            foreach (VisualElement child in groupBox.Children())
            {
                TemplateContainer previewTemplate = child.Q<TemplateContainer>("MeshPreview");
                if (i < meshes.Count && meshes[i] != null)
                {
                    previewTemplate.style.display = DisplayStyle.Flex;
                    CreatePreviewForMesh(meshes[i], previewTemplate, i);
                }
                else
                {
                    previewTemplate.style.display = DisplayStyle.None;
                }
                i++;
            }

            //Get combined info of all selected meshes
            if (meshes.Count > 1 && editorSettings.ShowInformationOnPreview)
            {
                allSelectedMeshCombinedDetails.style.display = DisplayStyle.Flex;
                UpdateMeshDataGroup(allSelectedMeshCombinedDetails.Q<TemplateContainer>("MeshDataGroup"), meshes);
            }
            else
            {
                allSelectedMeshCombinedDetails.style.display = DisplayStyle.None;
            }

            Label extraCount = root.Q<Label>("TotalSelectedObjectCount");
            if (meshes.Count < 5)
            {
                extraCount.style.display = DisplayStyle.None;
            }
            else
            {
                extraCount.style.display = DisplayStyle.Flex;
                extraCount.text = "+" + (meshes.Count - 4) + " more";
            }

            UpdatePreviewHeight();
        }

        private void CreatePreviewForMesh(Mesh mesh, TemplateContainer previewTemplate, int index)
        {
            previewTemplate.style.display = DisplayStyle.Flex;
            IMGUIContainer previewContainer = previewTemplate.Q<IMGUIContainer>("PreviewContainer");
            previewSettingsContainer = previewTemplate.Q<IMGUIContainer>("PreviewSettingsContainer");

            if (meshPreview[index] != null)
                meshPreview[index].Dispose();
            meshPreview[index] = new MeshPreview(mesh);

            previewContainer.style.display = DisplayStyle.Flex;
            previewSettingsContainer.style.display = DisplayStyle.Flex;

            previewContainer.onGUIHandler = null;
            previewSettingsContainer.onGUIHandler = null;

            GUIStyle style = new GUIStyle();
            style.normal.background = BackgroundTexture(editorSettings.PreviewBackgroundColor);

            //Draw preview
            if (meshPreview != null)
            {
                previewSettingsContainer.onGUIHandler = null;
                previewSettingsContainer.onGUIHandler += () =>
                {
                    //GUI.backgroundColor = new Color(1, 1, 1, 1f);
                    GUI.contentColor = Color.white;
                    GUI.color = Color.white;

                    GUILayout.BeginHorizontal("Box");
                    meshPreview[index].OnPreviewSettings();
                    GUILayout.EndHorizontal();
                };
                previewContainer.onGUIHandler = null;
                previewContainer.onGUIHandler += () =>
                {
                    if (previewContainer.contentRect.height <= 0)
                        previewContainer.style.height = 50;

                    if (previewContainer.contentRect.height > 0 && previewContainer.contentRect.width > 0) //Should be unnecessary. But still fixes a bug with height and width being negative.
                        meshPreview[index].OnPreviewGUI(previewContainer.contentRect, style);
                };
            }

            if (targets.Length > 1)
                UpdatePreviewMeshInformation(mesh, previewTemplate);
            else
                UpdatePreviewMeshInformation(mesh, previewTemplate, true);
        }

        private void UpdatePreviewMeshInformation(Mesh mesh, VisualElement container, bool showLearnMoreButton = false)
        {
            TemplateContainer meshDataGroup = container.Q<TemplateContainer>("MeshDataGroup");
            if (editorSettings.ShowInformationOnPreview)
            {
                meshDataGroup.style.display = DisplayStyle.Flex;
                UpdateMeshDataGroup(meshDataGroup, mesh);
            }
            else
            {
                meshDataGroup.style.display = DisplayStyle.None;
            }

            GroupBox meshMemoryGroupBox = container.Q<GroupBox>("RuntimeMemoryUsageOutsideFoldout");
            if (editorSettings.runtimeMemoryUsageUnderPreview)
            {
                meshMemoryGroupBox.style.display = DisplayStyle.Flex;
                meshMemoryGroupBox.Q<Label>("MemoryUsageInFoldout").text = GetMemoryUsage(mesh);

                if (showLearnMoreButton)
                {
                    meshMemoryGroupBox.Q<Button>().style.display = DisplayStyle.Flex;
                    meshMemoryGroupBox.Q<Button>().clicked += () => { Application.OpenURL(learnMoreAboutRuntimeMemoryUsageLink); };
                }
            }
            else
                meshMemoryGroupBox.style.display = DisplayStyle.None;
        }

        private Texture2D BackgroundTexture(Color color)
        {
            Texture2D newTexture = new Texture2D(1, 1);
            newTexture.SetPixel(0, 0, color);
            newTexture.Apply();
            return newTexture;
        }

        private void HideMeshPreview()
        {
            meshPreviewContainerGroupBox.style.display = DisplayStyle.None;
            root.Q<Label>("TotalSelectedObjectCount").style.display = DisplayStyle.None;

            Button inspectorSettingsButton2 = root.Q<Button>("InspectorSettingsButton2");
            inspectorSettingsButton2.style.display = DisplayStyle.Flex;
        }

        #endregion Preview

        #region Foldouts

        /// <summary>
        /// This is called at the beginning and when mesh is updated
        /// </summary>
        /// <param name="newMesh"></param>
        private void UpdateFoldouts()
        {
            UpdateInformationFoldout();

            if (targets.Length == 1)
            {
                UpdateSizeFoldout();
                UpdateButtonsFoldout();
                UpdateMeshDebugFoldout();
                //UpdateInspectorSettingsFoldout();
            }
        }

        private void SetupMeshDebugFoldout()
        {
            gizmoFoldout = root.Q<GroupBox>("MeshDebug");
            customFoldoutSetup.SetupFoldout(gizmoFoldout);
        }

        private void UpdateMeshDebugFoldout()
        {
            if (showDebugGizmoFoldout)
            {
                gizmoFoldout.style.display = DisplayStyle.Flex;
                DrawGizmoSettings(gizmoFoldout.Q<GroupBox>("Content"));
            }
            else gizmoFoldout.style.display = DisplayStyle.None;
        }

        private void SetupActionsFoldout()
        {
            actionsFoldout = root.Q<GroupBox>("Buttons");
            customFoldoutSetup.SetupFoldout(actionsFoldout);

            lightmapGenerateFoldout = actionsFoldout.Q<GroupBox>("GenerateSecondaryUVsetGroupBox");
            customFoldoutSetup.SetupFoldout(lightmapGenerateFoldout);
            lightmapGenerate_hardAngle = lightmapGenerateFoldout.Q<SliderInt>("HardAngleSlider");
            lightmapGenerate_angleError = lightmapGenerateFoldout.Q<SliderInt>("AngleErrorSlider");
            lightmapGenerate_areaError = lightmapGenerateFoldout.Q<SliderInt>("AreaErrorSlider");
            lightmapGenerate_packMargin = lightmapGenerateFoldout.Q<SliderInt>("PackMarginSlider");

            Button resetGenerateSecondaryUVSetButton = lightmapGenerateFoldout.Q<Button>("ResetGenerateSecondaryUVSetButton");
            resetGenerateSecondaryUVSetButton.clicked += () =>
            {
                UnwrapParam unwrapParam = new UnwrapParam();
                UnwrapParam.SetDefaults(out unwrapParam);

                lightmapGenerate_hardAngle.value = Mathf.CeilToInt(unwrapParam.hardAngle);
                lightmapGenerate_angleError.value = Mathf.CeilToInt(Remap(unwrapParam.angleError, 0, 1, 1, 75));
                lightmapGenerate_areaError.value = Mathf.CeilToInt(Remap(unwrapParam.areaError, 0, 1, 1, 75));
                lightmapGenerate_packMargin.value = Mathf.CeilToInt(Remap(unwrapParam.packMargin, 0, 1, 1, 64));
            };
        }

        private float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        private void UpdateButtonsFoldout()
        {
            if (showActionsFoldout)
            {
                actionsFoldout.style.display = DisplayStyle.Flex;
                UpdateFoldout_actionButtons();
            }
            else actionsFoldout.style.display = DisplayStyle.None;
        }

        private void SetupInformationFoldout()
        {
            informationFoldout = root.Q<GroupBox>("Informations");
            customFoldoutSetup.SetupFoldout(informationFoldout);

            assetLocationLabel = informationFoldout.Q<Label>("assetLocation");
            assetLocationInInformationFoldoutGroupBox = informationFoldout.Q<GroupBox>("AssetLocationInFoldoutGroupBox");

            runtimeMemoryUsageInInformationFoldout = informationFoldout.Q<GroupBox>("RuntimeMemoryUsageInInformation");
            memoryUsageInFoldout = runtimeMemoryUsageInInformationFoldout.Q<Label>("MemoryUsageInFoldout");
            informationFoldout.Q<Button>("RuntimeMemoryUsageExplaination").clicked += () => { Application.OpenURL(learnMoreAboutRuntimeMemoryUsageLink); };
        }

        private void UpdateInformationFoldout()
        {
            if (showInformationFoldout)
            {
                informationFoldout.style.display = DisplayStyle.Flex;
                if (informationFoldout.Q<Toggle>("FoldoutToggle").value)
                {
                    UpdateFoldout_informations();
                }
            }
            else informationFoldout.style.display = DisplayStyle.None;

            informationFoldout.Q<Toggle>("FoldoutToggle").RegisterValueChangedCallback(ev =>
            {
                if (ev.newValue)
                    UpdateFoldout_informations();
            });
        }

        private void SetupSizeFoldout()
        {
            sizeFoldout = root.Q<GroupBox>("MeshSize"); ;
            customFoldoutSetup.SetupFoldout(sizeFoldout);
        }

        private void UpdateSizeFoldout()
        {
            if (showSizeFoldout)
            {
                sizeFoldout.style.display = DisplayStyle.Flex;
                UpdateFoldout_information_meshSize(sizeFoldout.Q<GroupBox>("Content"));
            }
            else sizeFoldout.style.display = DisplayStyle.None;
        }

        private void HideAllFoldouts()
        {
            sizeFoldout ??= root.Q<GroupBox>("MeshSize"); ;
            actionsFoldout ??= root.Q<GroupBox>("Buttons");
            inspectorSettingsFoldout ??= root.Q<GroupBox>("InspectorSettings");
            gizmoFoldout ??= root.Q<GroupBox>("MeshDebug");

            informationFoldout.style.display = DisplayStyle.None;
            sizeFoldout.style.display = DisplayStyle.None;
            actionsFoldout.style.display = DisplayStyle.None;
            inspectorSettingsFoldout.style.display = DisplayStyle.None;
            gizmoFoldout.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// The asset location used by this method is updated via the UpdateMeshFilterField method
        /// </summary>
        private void UpdateFoldout_informations()
        {
            if (updatedInformation)
                return;
            updatedInformation = true;

            if (targets.Length == 1)
            {
                if (showAssetLocationInFoldout)
                {
                    assetLocationInInformationFoldoutGroupBox.style.display = DisplayStyle.Flex;
                    assetLocationLabel.text = assetPath;
                }
                else
                {
                    assetLocationInInformationFoldoutGroupBox.style.display = DisplayStyle.None;
                }

                if (editorSettings.runtimeMemoryUsageInInformationFoldout)
                {
                    runtimeMemoryUsageInInformationFoldout.style.display = DisplayStyle.Flex;
                    memoryUsageInFoldout.text = GetMemoryUsage(mesh);
                }
                else
                {
                    runtimeMemoryUsageInInformationFoldout.style.display = DisplayStyle.None;
                }
            }

            TemplateContainer meshDataGroup = informationFoldout.Q<TemplateContainer>("MeshDataGroup");
            UpdateMeshDataGroup(meshDataGroup, meshes);
        }

        /// <summary>
        /// This is for the main data
        /// </summary>
        /// <param name="meshDataGroup"></param>
        /// <param name="meshes"></param>
        private void UpdateMeshDataGroup(TemplateContainer meshDataGroup, List<Mesh> meshes)
        {
            GroupBox verticesGroup = meshDataGroup.Q<GroupBox>("VerticesGroup");
            if (!showVertexInformation) verticesGroup.style.display = DisplayStyle.None;
            else
            {
                verticesGroup.style.display = DisplayStyle.Flex;

                int counter = 0;
                foreach (Mesh mesh in meshes)
                {
                    if (mesh != null)
                        counter += mesh.vertexCount;
                }
                verticesGroup.Q<Label>("Value").text = counter.ToString();

                GroupBox submeshGroup = meshDataGroup.Q<GroupBox>("SubmeshGroup");
                if (meshes.Count == 1)
                {
                    int[] subMeshVertexCounts = meshes[0].SubMeshVertexCount();
                    if (subMeshVertexCounts != null && subMeshVertexCounts.Length > 1)
                    {
                        submeshGroup.style.display = DisplayStyle.Flex;
                        submeshGroup.Q<Label>("SubmeshValue").text = subMeshVertexCounts.Length.ToString();
                        Label submeshVertices = submeshGroup.Q<Label>("SubmeshVertices");
                        submeshVertices.text = "(";
                        for (int i = 0; i < subMeshVertexCounts.Length; i++)
                        {
                            submeshVertices.text += subMeshVertexCounts[i];
                            if (i + 1 != subMeshVertexCounts.Length)
                                submeshVertices.text += ", ";
                        }
                        submeshVertices.text += ")";
                    }
                    else
                    {
                        submeshGroup.style.display = DisplayStyle.None;
                    }
                }
                else
                {
                    submeshGroup.style.display = DisplayStyle.None;
                }
            }

            GroupBox trianglesGroup = meshDataGroup.Q<GroupBox>("TrianglesGroup");
            if (!showTriangleInformation) trianglesGroup.style.display = DisplayStyle.None;
            else
            {
                trianglesGroup.style.display = DisplayStyle.Flex;

                int counter = 0;
                foreach (Mesh mesh in meshes)
                {
                    if (mesh != null)
                        counter += mesh.TrianglesCount();
                }

                trianglesGroup.Q<Label>("Value").text = counter.ToString();
            }

            GroupBox edgeGroup = meshDataGroup.Q<GroupBox>("EdgeGroup");
            if (!showEdgeInformation) edgeGroup.style.display = DisplayStyle.None;
            else
            {
                edgeGroup.style.display = DisplayStyle.Flex;

                int counter = 0;
                foreach (Mesh mesh in meshes)
                {
                    if (mesh != null)
                        counter += mesh.EdgeCount();
                }

                edgeGroup.Q<Label>("Value").text = counter.ToString();
            }

            GroupBox tangentsGroup = meshDataGroup.Q<GroupBox>("TangentsGroup");
            if (!showTangentInformation) tangentsGroup.style.display = DisplayStyle.None;
            else
            {
                tangentsGroup.style.display = DisplayStyle.Flex;

                int counter = 0;
                foreach (Mesh mesh in meshes)
                {
                    if (mesh != null)
                        counter += mesh.tangents.Length;
                }

                tangentsGroup.Q<Label>("Value").text = counter.ToString();
            }

            GroupBox faceGroup = meshDataGroup.Q<GroupBox>("FaceGroup");
            if (!showFaceInformation) faceGroup.style.display = DisplayStyle.None;
            else
            {
                faceGroup.style.display = DisplayStyle.Flex;

                int counter = 0;
                foreach (Mesh mesh in meshes)
                {
                    if (mesh != null)
                        counter += mesh.FaceCount();
                }

                faceGroup.Q<Label>("Value").text = counter.ToString();
            }
        }

        /// <summary>
        /// When multiple meshes are selected, this is used to show mesh information of individual previews
        /// </summary>
        /// <param name="meshDataGroup"></param>
        /// <param name="newMesh"></param>
        private void UpdateMeshDataGroup(TemplateContainer meshDataGroup, Mesh newMesh)
        {
            GroupBox verticesGroup = meshDataGroup.Q<GroupBox>("VerticesGroup");
            if (!showVertexInformation) verticesGroup.style.display = DisplayStyle.None;
            else
            {
                verticesGroup.style.display = DisplayStyle.Flex;
                verticesGroup.Q<Label>("Value").text = newMesh.vertexCount.ToString();

                GroupBox submeshGroup = meshDataGroup.Q<GroupBox>("SubmeshGroup");
                int[] subMeshVertexCounts = newMesh.SubMeshVertexCount();
                if (subMeshVertexCounts != null && subMeshVertexCounts.Length > 1)
                {
                    submeshGroup.style.display = DisplayStyle.Flex;
                    submeshGroup.Q<Label>("SubmeshValue").text = subMeshVertexCounts.Length.ToString();
                    Label submeshVertices = submeshGroup.Q<Label>("SubmeshVertices");
                    submeshVertices.text = "(";
                    for (int i = 0; i < subMeshVertexCounts.Length; i++)
                    {
                        submeshVertices.text += subMeshVertexCounts[i];
                        if (i + 1 != subMeshVertexCounts.Length)
                            submeshVertices.text += ", ";
                    }
                    submeshVertices.text += ")";
                }
                else
                {
                    submeshGroup.style.display = DisplayStyle.None;
                }
            }

            GroupBox trianglesGroup = meshDataGroup.Q<GroupBox>("TrianglesGroup");
            if (!showTriangleInformation) trianglesGroup.style.display = DisplayStyle.None;
            else
            {
                trianglesGroup.style.display = DisplayStyle.Flex;
                trianglesGroup.Q<Label>("Value").text = newMesh.TrianglesCount().ToString();
            }

            GroupBox edgeGroup = meshDataGroup.Q<GroupBox>("EdgeGroup");
            if (!showEdgeInformation) edgeGroup.style.display = DisplayStyle.None;
            else
            {
                edgeGroup.style.display = DisplayStyle.Flex;
                edgeGroup.Q<Label>("Value").text = newMesh.EdgeCount().ToString();
            }

            GroupBox tangentsGroup = meshDataGroup.Q<GroupBox>("TangentsGroup");
            if (!showTangentInformation) tangentsGroup.style.display = DisplayStyle.None;
            else
            {
                tangentsGroup.style.display = DisplayStyle.Flex;
                tangentsGroup.Q<Label>("Value").text = newMesh.tangents.Length.ToString();
            }

            GroupBox faceGroup = meshDataGroup.Q<GroupBox>("FaceGroup");
            if (!showFaceInformation) faceGroup.style.display = DisplayStyle.None;
            else
            {
                faceGroup.style.display = DisplayStyle.Flex;
                faceGroup.Q<Label>("Value").text = newMesh.FaceCount().ToString();
            }
        }

        private void UpdateFoldout_information_meshSize(VisualElement container)
        {
            if (meshes.Count == 0)
                return;

            Mesh newMesh = meshes[0];
            if (newMesh == null) return;

            DropdownField meshUnitDropdown = container.parent.Q<DropdownField>("MeshUnit");

            if (ScalesFinder.MyScales().GetAvailableUnits().ToList().Count == 0) ScalesFinder.MyScales().Reset();
            meshUnitDropdown.choices = ScalesFinder.MyScales().GetAvailableUnits().ToList();

            meshUnitDropdown.index = editorSettings.SelectedUnit;

            meshUnitDropdown.RegisterCallback<ChangeEvent<string>>(ev =>
            {
                editorSettings.SelectedUnit = meshUnitDropdown.index;
                updateValues(" " + ev.newValue);
            });

            updateValues(" " + ScalesFinder.MyScales().GetAvailableUnits()[editorSettings.SelectedUnit]);

            void updateValues(string selectedUnitName)
            {
                //selectedUnitName = " " + ScaleSettings.GetAvailableUnits()[selectedUnit];
                editorSettings.SelectedUnit = meshUnitDropdown.index;
                Bounds meshBound = newMesh.MeshSizeEditorOnly(ScalesFinder.MyScales().CurrentUnitValue());

                TemplateContainer meshSizeTemplateContainer = root.Q<TemplateContainer>("MeshSize");

                GroupBox lengthGroup = meshSizeTemplateContainer.Q<GroupBox>("LengthGroup");
                Label lengthValue = lengthGroup.Q<Label>("Value");
                lengthValue.text = RoundedFloat(meshBound.size.x).ToString();
                lengthValue.tooltip = meshBound.size.x.ToString();

                GroupBox heightGroup = meshSizeTemplateContainer.Q<GroupBox>("HeightGroup");
                Label heightValue = heightGroup.Q<Label>("Value");
                heightValue.text = RoundedFloat(meshBound.size.y).ToString();
                heightValue.tooltip = meshBound.size.y.ToString();

                GroupBox depthGroup = meshSizeTemplateContainer.Q<GroupBox>("DepthGroup");
                Label depthValue = depthGroup.Q<Label>("Value");
                depthValue.text = RoundedFloat(meshBound.size.z).ToString();
                depthValue.tooltip = meshBound.size.z.ToString();

                Label centerLabel = root.Q<Label>("Center");
                string centertext = RoundedFloat(meshBound.center.x) + ", " + RoundedFloat(meshBound.center.y) + ", " + RoundedFloat(meshBound.center.z);
                centerLabel.text = centertext;
                centerLabel.tooltip = "Number is rounded after 4 digits";
            }

            float RoundedFloat(float rawFloat) => (float)System.Math.Round(rawFloat, 4);
        }

        private void UpdateFoldout_actionButtons()
        {
            Toggle doNotApplyActionToAssetToggle = root.Q<Toggle>("doNotApplyActionToAsset");
            doNotApplyActionToAssetToggle.value = doNotApplyActionToAsset;
            doNotApplyActionToAssetToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.DoNotApplyActionToAsset = ev.newValue;
                doNotApplyActionToAsset = ev.newValue;
            });

            Button optimizeMesh = root.Q<Button>("OptimizeMesh");
            if (showOptimizeButton) optimizeMesh.style.display = DisplayStyle.Flex;
            else optimizeMesh.style.display = DisplayStyle.None;
            optimizeMesh.RegisterCallback<ClickEvent>(MeshInsnanceCheck);
            optimizeMesh.RegisterCallback<ClickEvent>(OptimizeMesh);

            Button recalculateNormals = root.Q<Button>("RecalculateNormals");
            if (showRecalculateNormalsButton) recalculateNormals.style.display = DisplayStyle.Flex;
            else recalculateNormals.style.display = DisplayStyle.None;
            recalculateNormals.RegisterCallback<ClickEvent>(MeshInsnanceCheck);
            recalculateNormals.RegisterCallback<ClickEvent>(RecalculateNormals);

            Button recalculateTangents = root.Q<Button>("RecalculateTangents");
            if (showRecalculateTangentsButton) recalculateTangents.style.display = DisplayStyle.Flex;
            else recalculateTangents.style.display = DisplayStyle.None;
            recalculateTangents.RegisterCallback<ClickEvent>(MeshInsnanceCheck);
            recalculateTangents.RegisterCallback<ClickEvent>(RecalculateTangents);

            Button flipNormals = root.Q<Button>("FlipNormals");
            if (showFlipNormalsButton) flipNormals.style.display = DisplayStyle.Flex;
            else flipNormals.style.display = DisplayStyle.None;
            flipNormals.RegisterCallback<ClickEvent>(MeshInsnanceCheck);
            flipNormals.RegisterCallback<ClickEvent>(FlipNormals);

            Button generateSecondaryUVSet = root.Q<Button>("GenerateSecondaryUVSet");
            if (showGenerateSecondaryUVButton) lightmapGenerateFoldout.style.display = DisplayStyle.Flex;
            else lightmapGenerateFoldout.style.display = DisplayStyle.None;
            generateSecondaryUVSet.RegisterCallback<ClickEvent>(MeshInsnanceCheck);
            generateSecondaryUVSet.RegisterCallback<ClickEvent>(GenerateSecondaryUVSet);

            Button saveMeshAsField = root.Q<Button>("exportMesh");
            if (showSaveMeshButtonAs) saveMeshAsField.style.display = DisplayStyle.Flex;
            else saveMeshAsField.style.display = DisplayStyle.None;
            saveMeshAsField.RegisterCallback<ClickEvent>(ExportMesh);

            /// <summary>
            /// This is used to make sure the mesh you are modifying is an instance and the user isn't accidentally modifying the asset
            /// </summary>
            void MeshInsnanceCheck(ClickEvent @event)
            {
                if (MeshIsAnAsset(mesh) && doNotApplyActionToAsset)
                {
                    Mesh newMesh = new Mesh();
                    newMesh.vertices = mesh.vertices;
                    newMesh.triangles = mesh.triangles;
                    newMesh.uv = mesh.uv;
                    newMesh.normals = mesh.normals;
                    newMesh.name = mesh.name + " (Local Instance)";
                    Undo.RecordObject(sourceMeshFilter, "Mesh instance creation");
                    sourceMeshFilter.mesh = newMesh;
                    EditorUtility.SetDirty(sourceMeshFilter);
                    mesh = newMesh;
                }

                //UpdateFoldout_importSettings(importSettings.Q<GroupBox>("Container"), mesh);
            }

            //void SubDivideMesh(ClickEvent evt)
            //{
            //    sourceMeshFilter.sharedMesh = mesh.SubDivide();
            //    mesh = sourceMeshFilter.sharedMesh;
            //    EditorUtility.SetDirty(mesh);
            //    Log("exported");
            //    UpdateFoldouts(mesh);
            //    SceneView.RepaintAll();
            //}

            void OptimizeMesh(ClickEvent evt)
            {
                Undo.RecordObject(sourceMeshFilter.gameObject, "Optimize mesh");
                mesh.Optimize();
                Log("optimized");
                UpdateFoldouts();
            }

            void RecalculateNormals(ClickEvent evt)
            {
                Undo.RecordObject(mesh, "Modifying Normals");
                mesh.RecalculateNormals();
                EditorUtility.SetDirty(mesh);
                Log("normals recalculated");
                UpdateFoldouts();
            }

            void RecalculateTangents(ClickEvent evt)
            {
                Undo.RecordObject(mesh, "Modifying Tangents");
                mesh.RecalculateTangents();
                EditorUtility.SetDirty(mesh);
                Log("tangents recalculated");
                UpdateFoldouts();
            }

            void FlipNormals(ClickEvent evt)
            {
                mesh.FlipNormals();
                EditorUtility.SetDirty(mesh);
                Log("normals flipped");
                UpdateFoldouts();
                SceneView.RepaintAll();
            }

            /// <summary>
            /// Compute a unique UV layout for a Mesh, and store it in Mesh.uv2.
            /// When you import a model asset, you can instruct Unity to compute a light map UV layout for it using [[ModelImporter-generateSecondaryUV]] or the Model Import Settings Inspector. This function allows you to do the same to procedurally generated meshes.
            ///If this process requires multiple UV charts to flatten the mesh, the mesh might contain more vertices than before. If the mesh uses 16-bit indices (see Mesh.indexFormat) and the process would result in more vertices than are possible to use with 16-bit indices, this function fails and returns false.
            /// Note: Editor only
            /// </summary>
            void GenerateSecondaryUVSet(ClickEvent evt)
            {
                UnwrapParam unwrapParam = new UnwrapParam();
                UnwrapParam.SetDefaults(out unwrapParam);

                unwrapParam.hardAngle = lightmapGenerate_hardAngle.value;
                unwrapParam.angleError = Remap(lightmapGenerate_angleError.value, 1f, 75f, 0f, 1f);
                unwrapParam.areaError = Remap(lightmapGenerate_areaError.value, 1f, 75f, 0f, 1f);
                unwrapParam.packMargin = Remap(lightmapGenerate_packMargin.value, 0f, 1f, 0f, 64f);

                Undo.RecordObject(mesh, "Modifying Normals");
                Unwrapping.GenerateSecondaryUVSet(mesh, unwrapParam);
                EditorUtility.SetDirty(mesh);
                Log("For Light-mapping, Secondary UV set generated");
                UpdateFoldouts();
                SceneView.RepaintAll();
            }

            void ExportMesh(ClickEvent evt)
            {
                sourceMeshFilter.sharedMesh = mesh.ExportMesh();
                mesh = sourceMeshFilter.sharedMesh;
                EditorUtility.SetDirty(mesh);
                UpdateFoldouts();
                SceneView.RepaintAll();
            }
        }

        private void UpdatePreviewHeight()
        {
            //When mesh preview was turned off, the line below caused empty preview panels to become visible when settings foldout was opened.
            //meshPreviewContainerGroupBox.style.display = DisplayStyle.Flex;
            GroupBox groupBox = meshPreviewContainerGroupBox.Q<GroupBox>("PreviewsGroupBox");

            foreach (VisualElement child in groupBox.Children())
            {
                TemplateContainer previewTemplate = child.Q<TemplateContainer>("MeshPreview");
                previewTemplate.Q<IMGUIContainer>("PreviewContainer").style.height = CorrectedHeight();
            }
        }

        private float CorrectedHeight()
        {
            if (editorSettings.MeshPreviewHeight == 0) return 2;

            return Mathf.Abs(editorSettings.MeshPreviewHeight);
        }

        private void GetEditorSettings()
        {
            editorSettings = BetterMeshSettings.instance;

            showMeshPreview = editorSettings.ShowMeshPreview;

            showInformationFoldout = editorSettings.ShowInformationFoldout;

            showVertexInformation = editorSettings.ShowVertexInformation;
            showTriangleInformation = editorSettings.ShowTriangleInformation;
            showEdgeInformation = editorSettings.ShowEdgeInformation;
            showFaceInformation = editorSettings.ShowFaceInformation;
            showTangentInformation = editorSettings.ShowTangentInformation;

            showSizeFoldout = editorSettings.ShowSizeFoldout;
            showAssetLocationInFoldout = editorSettings.ShowAssetLocationInFoldout;

            showDebugGizmoFoldout = editorSettings.ShowDebugGizmoFoldout;

            showActionsFoldout = editorSettings.ShowActionsFoldout;

            showOptimizeButton = editorSettings.ShowOptimizeButton;
            showRecalculateNormalsButton = editorSettings.ShowRecalculateNormalsButton;
            showRecalculateTangentsButton = editorSettings.ShowRecalculateTangentsButton;
            showFlipNormalsButton = editorSettings.ShowFlipNormalsButton;
            showGenerateSecondaryUVButton = editorSettings.ShowGenerateSecondaryUVButton;
            showSaveMeshButtonAs = editorSettings.ShowSaveMeshAsButton;

            doNotApplyActionToAsset = editorSettings.DoNotApplyActionToAsset;
        }

        #endregion Foldouts

        #region Settings

        #region Variables

        private GroupBox inspectorSettingsFoldout;
        private Toggle inspectorSettingsFoldoutToggle;

        private bool inspectorFoldoutSetupCompleted = false;

        private Toggle autoHideInspectorSettingsField;

        private Toggle overrideInspectorColorToggle;
        private ColorField inspectorColorField;
        private Toggle overrideFoldoutColorToggle;
        private ColorField foldoutColorField;

        private Toggle showMeshPreviewField;
        private FloatField meshPreviewHeightField;
        private Toggle showInformationOnPreviewField;
        private Toggle showAssetLocationBelowPreviewField;
        private Toggle showRuntimeMemoryUsageInFoldout;
        private Toggle showRuntimeMemoryUsageBelowPreview;

        private Toggle showMeshSizeField;
        private Toggle showAssetLocationInFoldoutToggle;

        private Toggle showMeshDetailsInFoldoutToggle;
        private Toggle showVertexInformationToggle;
        private Toggle showTriangleInformationToggle;
        private Toggle showEdgeInformationToggle;
        private Toggle showFaceInformationToggle;
        private Toggle showTangentInformationToggle;

        private Toggle showActionsFoldoutField;
        private Toggle showOptimizeButtonToggle;
        private Toggle recalculateNormalsToggle;
        private Toggle showRecalculateTangentsButtonToggle;
        private Toggle showFlipNormalsToggle;
        private Toggle showGenerateSecondaryUVButtonToggle;
        private Toggle showSaveMeshButtonAsToggle;

        private Toggle showDebugGizmoFoldoutField;

        #endregion Variables

        #region Setup

        /// <summary>
        /// This setup everything that requires the settings toggle button to work
        /// </summary>
        private void SetupInspectorSettingsFoldout()
        {
            inspectorFoldoutSetupCompleted = false;

            inspectorSettingsFoldout = root.Q<GroupBox>("InspectorSettings");
            customFoldoutSetup.SetupFoldout(inspectorSettingsFoldout);

            inspectorSettingsFoldout.style.display = DisplayStyle.None;

            //These are the buttons that toggle on/off the foldout visibility
            SetupInspectorSettingsButtons();

            foldoutColorField = inspectorSettingsFoldout.Q<ColorField>("FoldoutColorField");
            inspectorColorField = inspectorSettingsFoldout.Q<ColorField>("InspectorColorField");
        }

        /// <summary>
        /// These are the buttons that toggle on/off the foldout visibility
        /// </summary>
        private void SetupInspectorSettingsButtons()
        {
            Button inspectorSettingsButton = root.Q<Button>("InspectorSettingsButton");
            inspectorSettingsButton.RegisterCallback<ClickEvent>(ToggleInspectorSettings);

            Button inspectorSettingsButton2 = root.Q<Button>("InspectorSettingsButton2");
            inspectorSettingsButton2.RegisterCallback<ClickEvent>(ToggleInspectorSettings);

            if (showMeshPreview) inspectorSettingsButton2.style.display = DisplayStyle.None;
            else inspectorSettingsButton2.style.display = DisplayStyle.Flex;
        }

        private void SetupInspectorSettingsFoldoutCompletely()
        {
            customFoldoutSetup.SetupFoldout(inspectorSettingsFoldout.Q<GroupBox>("InspectorCustomizationFoldout"));
            customFoldoutSetup.SetupFoldout(inspectorSettingsFoldout.Q<GroupBox>("MeshPreviewSettingsFoldout"), "FoldoutToggle", "showMeshPreview");
            customFoldoutSetup.SetupFoldout(inspectorSettingsFoldout.Q<GroupBox>("InformationFoldoutSettingsFoldout"), "FoldoutToggle", "ShowMeshDetailsInFoldoutToggle");
            customFoldoutSetup.SetupFoldout(inspectorSettingsFoldout.Q<GroupBox>("MeshDetailsSettingsFoldout"));
            customFoldoutSetup.SetupFoldout(inspectorSettingsFoldout.Q<GroupBox>("ActionSettingsFoldout"), "FoldoutToggle", "showActionsFoldout");

            inspectorSettingsFoldoutToggle = inspectorSettingsFoldout.Q<Toggle>("FoldoutToggle");
            inspectorSettingsFoldoutToggle.RegisterValueChangedCallback(ev =>
            {
                if (editorSettings.AutoHideSettings)
                {
                    if (ev.newValue)
                    {
                        inspectorSettingsFoldout.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        inspectorSettingsFoldout.style.display = DisplayStyle.None;
                    }
                }
            });
            SetupFooter();

            #region Reset

            inspectorSettingsFoldout.Q<Button>("ResetInspectorSettings").clicked += ResetInspectorSettings;
            inspectorSettingsFoldout.Q<Button>("ResetInspectorSettings2").clicked += ResetInspectorSettings2;
            inspectorSettingsFoldout.Q<Button>("ResetInspectorSettingsToMinimal").clicked += ResetInspectorSettingsToMinimal;
            inspectorSettingsFoldout.Q<Button>("ResetInspectorSettingsToNothing").clicked += ResetInspectorSettingsToNothing;

            #endregion Reset

            autoHideInspectorSettingsField = inspectorSettingsFoldout.Q<Toggle>("autoHideInspectorSettings");
            autoHideInspectorSettingsField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.AutoHideSettings = ev.newValue;
            });
            var MeshFieldOnTop = inspectorSettingsFoldout.Q<Toggle>("MeshFieldOnTop");
            MeshFieldOnTop.RegisterValueChangedCallback(ev =>
            {
                editorSettings.meshFieldOnTop = ev.newValue;
                UpdateMeshFieldGroupPosition();
            });

            #region Inspector Customization

            overrideInspectorColorToggle = inspectorSettingsFoldout.Q<Toggle>("OverrideInspectorColorToggle");
            overrideInspectorColorToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.OverrideInspectorColor = ev.newValue;
                UpdateInspectorColor();
            });
            inspectorColorField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.InspectorColor = ev.newValue;
                UpdateInspectorColor();
            });
            overrideFoldoutColorToggle = inspectorSettingsFoldout.Q<Toggle>("OverrideFoldoutColorToggle");
            overrideFoldoutColorToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.OverrideFoldoutColor = ev.newValue;
                UpdateInspectorColor();
            });
            foldoutColorField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.FoldoutColor = ev.newValue;

                UpdateInspectorColor();
            });

            #endregion Inspector Customization

            #region Preview Settings

            showMeshPreviewField = inspectorSettingsFoldout.Q<Toggle>("showMeshPreview");

            meshPreviewHeightField = inspectorSettingsFoldout.Q<FloatField>("meshPreviewHeight");
            meshPreviewHeightField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.MeshPreviewHeight = ev.newValue;

                UpdatePreviewHeight();
            });
            showInformationOnPreviewField = inspectorSettingsFoldout.Q<Toggle>("ShowInformationOnPreview");
            showInformationOnPreviewField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowInformationOnPreview = ev.newValue;
                UpdatePreviewMeshInformation(mesh, meshPreviewContainerGroupBox);
            });
            showAssetLocationBelowPreviewField = inspectorSettingsFoldout.Q<Toggle>("ShowAssetLocationBelowPreview");
            showAssetLocationBelowPreviewField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowAssetLocationBelowMesh = ev.newValue;

                if (ev.newValue) assetLocationOutsideFoldout.style.display = DisplayStyle.Flex;
                else assetLocationOutsideFoldout.style.display = DisplayStyle.None;
            });

            showRuntimeMemoryUsageBelowPreview = inspectorSettingsFoldout.Q<Toggle>("ShowRuntimeMemoryUsageBelowPreview");
            showRuntimeMemoryUsageInFoldout = inspectorSettingsFoldout.Q<Toggle>("ShowRuntimeMemoryUsageInFoldout");
            showRuntimeMemoryUsageInFoldout.RegisterValueChangedCallback(ev =>
            {
                editorSettings.runtimeMemoryUsageInInformationFoldout = ev.newValue;
                editorSettings.Save();

                if (ev.newValue) runtimeMemoryUsageInInformationFoldout.style.display = DisplayStyle.Flex;
                else runtimeMemoryUsageInInformationFoldout.style.display = DisplayStyle.None;
            });
            showRuntimeMemoryUsageBelowPreview.RegisterValueChangedCallback(ev =>
            {
                editorSettings.runtimeMemoryUsageUnderPreview = ev.newValue;
                editorSettings.Save();
                UpdateMeshPreviews();
            });

            showMeshPreviewField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowMeshPreview = ev.newValue;
                showMeshPreview = ev.newValue;

                if (showMeshPreview)
                {
                    //meshPreviewHeightField.SetEnabled(true);
                    //showInformationOnPreviewField.SetEnabled(true);
                    //showAssetLocationBelowPreviewField.SetEnabled(true);
                    UpdateMeshPreviews();
                }
                else
                {
                    //meshPreviewHeightField.SetEnabled(false);
                    //showInformationOnPreviewField.SetEnabled(false);
                    //showAssetLocationBelowPreviewField.SetEnabled(false);
                    HideMeshPreview();
                }
            });

            #endregion Preview Settings

            showMeshSizeField = inspectorSettingsFoldout.Q<Toggle>("ShowMeshSize");
            showMeshSizeField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowSizeFoldout = ev.newValue;
                showSizeFoldout = ev.newValue;

                if (showSizeFoldout) sizeFoldout.style.display = DisplayStyle.Flex;
                else sizeFoldout.style.display = DisplayStyle.None;
            });

            showAssetLocationInFoldoutToggle = inspectorSettingsFoldout.Q<Toggle>("ShowAssetLocationInFoldout");
            showAssetLocationInFoldoutToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowAssetLocationInFoldout = ev.newValue;
                this.showAssetLocationInFoldout = ev.newValue;

                if (this.showAssetLocationInFoldout) assetLocationInInformationFoldoutGroupBox.style.display = DisplayStyle.Flex;
                else assetLocationInInformationFoldoutGroupBox.style.display = DisplayStyle.None;
            });

            #region Mesh Details

            showMeshDetailsInFoldoutToggle = inspectorSettingsFoldout.Q<Toggle>("ShowMeshDetailsInFoldoutToggle");
            showMeshDetailsInFoldoutToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowInformationFoldout = ev.newValue;
                showInformationFoldout = ev.newValue;

                if (showInformationFoldout) informationFoldout.style.display = DisplayStyle.Flex;
                else informationFoldout.style.display = DisplayStyle.None;
            });

            showVertexInformationToggle = inspectorSettingsFoldout.Q<Toggle>("showVertextCount");
            showVertexInformationToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowVertexInformation = ev.newValue;
                showVertexInformation = ev.newValue;

                updatedInformation = false;
                UpdateFoldout_informations();
            });
            showTriangleInformationToggle = inspectorSettingsFoldout.Q<Toggle>("showTriangleCount");
            showTriangleInformationToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowTriangleInformation = ev.newValue;
                showTriangleInformation = ev.newValue;

                updatedInformation = false;
                UpdateFoldout_informations();
            });
            showEdgeInformationToggle = inspectorSettingsFoldout.Q<Toggle>("showEdgeCount");
            showEdgeInformationToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowEdgeInformation = ev.newValue;
                showEdgeInformation = ev.newValue;

                updatedInformation = false;
                UpdateFoldout_informations();
            });

            showFaceInformationToggle = inspectorSettingsFoldout.Q<Toggle>("showFaceCount");
            showFaceInformationToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowFaceInformation = ev.newValue;
                showFaceInformation = ev.newValue;

                updatedInformation = false;
                UpdateFoldout_informations();
            });
            showTangentInformationToggle = inspectorSettingsFoldout.Q<Toggle>("showTangentCount");
            showTangentInformationToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowTangentInformation = ev.newValue;
                showTangentInformation = ev.newValue;

                updatedInformation = false;
                UpdateFoldout_informations();
            });

            #endregion Mesh Details

            #region Actions

            showActionsFoldoutField = inspectorSettingsFoldout.Q<Toggle>("showActionsFoldout");
            showActionsFoldoutField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowActionsFoldout = ev.newValue;
                showActionsFoldout = ev.newValue;

                if (showActionsFoldout) actionsFoldout.style.display = DisplayStyle.Flex;
                else actionsFoldout.style.display = DisplayStyle.None;
            });
            showOptimizeButtonToggle = inspectorSettingsFoldout.Q<Toggle>("OptimizeMesh");
            showOptimizeButtonToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowOptimizeButton = ev.newValue;
                showOptimizeButton = ev.newValue;

                UpdateFoldout_actionButtons();
            });
            recalculateNormalsToggle = inspectorSettingsFoldout.Q<Toggle>("RecalculateNormals");
            recalculateNormalsToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowRecalculateNormalsButton = ev.newValue;
                showRecalculateNormalsButton = ev.newValue;

                UpdateFoldout_actionButtons();
            });
            showRecalculateTangentsButtonToggle = inspectorSettingsFoldout.Q<Toggle>("RecalculateTangents");
            showRecalculateTangentsButtonToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowRecalculateTangentsButton = ev.newValue;
                showRecalculateTangentsButton = ev.newValue;

                UpdateFoldout_actionButtons();
            });
            showFlipNormalsToggle = inspectorSettingsFoldout.Q<Toggle>("FlipNormals");
            showFlipNormalsToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowFlipNormalsButton = ev.newValue;
                showFlipNormalsButton = ev.newValue;

                UpdateFoldout_actionButtons();
            });
            showGenerateSecondaryUVButtonToggle = inspectorSettingsFoldout.Q<Toggle>("GenerateSecondaryUVSet");
            showGenerateSecondaryUVButtonToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowGenerateSecondaryUVButton = ev.newValue;
                showGenerateSecondaryUVButton = ev.newValue;

                UpdateFoldout_actionButtons();
            });
            showSaveMeshButtonAsToggle = inspectorSettingsFoldout.Q<Toggle>("SaveMeshAs");
            showSaveMeshButtonAsToggle.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowSaveMeshAsButton = ev.newValue;
                showSaveMeshButtonAs = ev.newValue;

                UpdateFoldout_actionButtons();
            });

            #endregion Actions

            showDebugGizmoFoldoutField = inspectorSettingsFoldout.Q<Toggle>("showDebugGizmoFoldout");
            showDebugGizmoFoldoutField.RegisterValueChangedCallback(ev =>
            {
                editorSettings.ShowDebugGizmoFoldout = ev.newValue;
                showDebugGizmoFoldout = ev.newValue;

                if (showDebugGizmoFoldout) gizmoFoldout.style.display = DisplayStyle.Flex;
                else gizmoFoldout.style.display = DisplayStyle.None;
            });

            Button scaleSettingsButton = inspectorSettingsFoldout.Q<Button>("ScaleSettingsButton");
            scaleSettingsButton.clicked += () =>
            {
                SettingsService.OpenProjectSettings("Project/Tiny Giant Studio/Scale Settings");
                GUIUtility.ExitGUI();
            };

            // This indicates that the full setup doesn't need to be repeated
            inspectorFoldoutSetupCompleted = true;
        }

        #region Footer

        private readonly string assetLink = "https://assetstore.unity.com/packages/tools/utilities/better-mesh-filter-266489?aid=1011ljxWe";
        private readonly string publisherLink = "https://assetstore.unity.com/publishers/45848?aid=1011ljxWe";
        private readonly string documentationLink = "https://ferdowsur.gitbook.io/better-mesh/";

        private void SetupFooter()
        {
            root.Q<ToolbarButton>("AssetLink").clicked += () => { Application.OpenURL(assetLink); };
            root.Q<ToolbarButton>("Documentation").clicked += () => { Application.OpenURL(documentationLink); };
            root.Q<ToolbarButton>("OtherAssetsLink").clicked += () => { Application.OpenURL(publisherLink); };
        }

        #endregion Footer

        #endregion Setup

        private void UpdateInspectorSettingsFoldout()
        {
            autoHideInspectorSettingsField.value = editorSettings.AutoHideSettings;

            SetupInspectorCustomizationSettings();

            #region Preview

            showMeshPreviewField.value = showMeshPreview;
            showInformationOnPreviewField.value = editorSettings.ShowInformationOnPreview;
            meshPreviewHeightField.value = editorSettings.MeshPreviewHeight;
            UpdatePreviewHeight();

            showAssetLocationBelowPreviewField.value = editorSettings.ShowAssetLocationBelowMesh;
            showRuntimeMemoryUsageInFoldout.value = editorSettings.runtimeMemoryUsageInInformationFoldout;
            showRuntimeMemoryUsageBelowPreview.value = editorSettings.runtimeMemoryUsageUnderPreview;

            #endregion Preview

            #region Size

            showMeshSizeField.value = showSizeFoldout;
            showAssetLocationInFoldoutToggle.value = this.showAssetLocationInFoldout;

            #endregion Size

            #region Mesh Details

            showMeshDetailsInFoldoutToggle.value = showInformationFoldout;
            showVertexInformationToggle.value = showVertexInformation;
            showTriangleInformationToggle.value = showTriangleInformation;
            showEdgeInformationToggle.value = showEdgeInformation;
            showFaceInformationToggle.value = showFaceInformation;
            showTangentInformationToggle.value = showTangentInformation;

            #endregion Mesh Details

            #region Actions

            showActionsFoldoutField.value = showActionsFoldout;
            showOptimizeButtonToggle.value = showOptimizeButton;
            recalculateNormalsToggle.value = showRecalculateNormalsButton;
            showRecalculateTangentsButtonToggle.value = showRecalculateTangentsButton;
            showFlipNormalsToggle.value = showFlipNormalsButton;
            showGenerateSecondaryUVButtonToggle.value = showGenerateSecondaryUVButton;
            showSaveMeshButtonAsToggle.value = showSaveMeshButtonAs;

            #endregion Actions

            showDebugGizmoFoldoutField.value = showDebugGizmoFoldout;
        }

        private void ResetInspectorSettings()
        {
            editorSettings.ResetToDefault();
            EditorSettingsResetted();
        }

        private void ResetInspectorSettings2()
        {
            editorSettings.ResetToDefault2();
            EditorSettingsResetted();
        }

        private void ResetInspectorSettingsToMinimal()
        {
            editorSettings.ResetToMinimal();
            EditorSettingsResetted();
        }

        private void ResetInspectorSettingsToNothing()
        {
            editorSettings.ResetToNothing();
            EditorSettingsResetted();
        }

        private void EditorSettingsResetted()
        {
            GetEditorSettings();

            UpdateInspectorSettingsFoldout();
            UpdateInspector();
            UpdateInspectorColor();
        }

        private void SetupInspectorCustomizationSettings()
        {
            overrideInspectorColorToggle.value = editorSettings.OverrideInspectorColor;
            inspectorColorField.value = editorSettings.InspectorColor;
            overrideFoldoutColorToggle.value = editorSettings.OverrideFoldoutColor;
            foldoutColorField.value = editorSettings.FoldoutColor;

            UpdateInspectorColor();
        }

        private void UpdateInspectorColor()
        {
            List<GroupBox> customFoldoutGroups = root.Query<GroupBox>(className: "custom-foldout").ToList();
            if (editorSettings.OverrideFoldoutColor)
            {
                foldoutColorField.SetEnabled(true);

                foreach (GroupBox foldout in customFoldoutGroups)
                    foldout.style.backgroundColor = editorSettings.FoldoutColor;
            }
            else
            {
                foldoutColorField.SetEnabled(false);
                foreach (GroupBox foldout in customFoldoutGroups)
                    foldout.style.backgroundColor = StyleKeyword.Null;
            }

            if (editorSettings.OverrideInspectorColor)
            {
                inspectorColorField.SetEnabled(true);
                root.Q<GroupBox>("RootHolder").style.backgroundColor = editorSettings.InspectorColor;
            }
            else
            {
                inspectorColorField.SetEnabled(false);
                root.Q<GroupBox>("RootHolder").style.backgroundColor = StyleKeyword.Null;
            }
        }

        private void ToggleInspectorSettings(ClickEvent evt)
        {
            //if (inspectorSettingsToggledOff)
            if (inspectorSettingsFoldout.style.display == DisplayStyle.None)
            {
                //This completes the setup of the inspector foldout.
                //This is made to avoid referencing everything at the start,
                //Giving a bit performance improvement
                if (!inspectorFoldoutSetupCompleted) SetupInspectorSettingsFoldoutCompletely();

                UpdateInspectorSettingsFoldout();

                inspectorSettingsFoldout.style.display = DisplayStyle.Flex;
                inspectorSettingsFoldoutToggle.value = true;
            }
            else
            {
                inspectorSettingsFoldout.style.display = DisplayStyle.None;
                inspectorSettingsFoldoutToggle.value = false;
            }
        }

        #endregion Settings

        #region Functions

        /// <summary>
        /// This cleans up memory for the previews, textures and editors that can be created by the asset
        /// </summary>
        private void CleanUpUnusedMemory()
        {
            for (int i = 0; i < meshPreview.Length; i++)
            {
                meshPreview[i]?.Dispose();
            }

            if (originalEditor != null)
                DestroyImmediate(originalEditor);

            ResetMaterial();
        }

        private void ResetMaterial()
        {
            //if(!editorRepainted) { editorRepainted = true; return; }

            //Debug.Log(originalMaterial);

            //if (checkerMaterial != null)
            //    if (sourceMeshFilter.GetComponent<MeshRenderer>())
            //        if (sourceMeshFilter.GetComponent<MeshRenderer>().material = checkerMaterial)
            //            if (originalMaterial)
            //                sourceMeshFilter.GetComponent<MeshRenderer>().material = originalMaterial;
        }

        /// <summary>
        /// If the UXML file is missing for any reason,
        /// Instead of showing an empty inspector,
        /// This loads the default one.
        /// This shouldn't ever happen.
        /// </summary>
        private void LoadDefaultEditor()
        {
            if (originalEditor != null)
                DestroyImmediate(originalEditor);

            originalEditor = Editor.CreateEditor(targets);
            IMGUIContainer inspectorContainer = new IMGUIContainer(OnGUICallback);
            root.Add(inspectorContainer);
        }

        //For the original Editor
        private void OnGUICallback()
        {
            //EditorGUIUtility.hierarchyMode = true;

            EditorGUI.BeginChangeCheck();
            originalEditor.OnInspectorGUI();
            EditorGUI.EndChangeCheck();
        }

        private bool MeshIsAnAsset(Mesh newMesh) => AssetDatabase.Contains(newMesh);

        private Texture2D CreateCheckeredTexture(int width, int height, int checkSize)
        {
            Texture2D texture = new Texture2D(width, height);
            Color color1 = Color.black;
            Color color2 = Color.white;

            // Loop over the width and height.
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    // Determine which color to use based on the current x and y indices.
                    bool checkX = x / checkSize % 2 == 0;
                    bool checkY = y / checkSize % 2 == 0;
                    Color color = (checkX == checkY) ? color1 : color2;

                    // Set the pixel color.
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            return texture;
        }

        public string GetMemoryUsage(Mesh mesh)
        {
            if (mesh == null) return "";

            long usageInByte = GetMemoryUsageInByte(mesh);

            long megabytes = usageInByte / 1024;
            long remainingKilobytes = usageInByte % 1024;

            string usage = "";
            if (megabytes > 0) usage += megabytes + "MB ";
            if (remainingKilobytes > 0) usage += remainingKilobytes + "KB";

            return usage;
        }

        private static long GetMemoryUsageInByte(Mesh mesh)
        {
            return UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh);
        }

        #endregion Functions

        /// <summary>
        /// This is used by actions to create a colorful debug log without having to rewrite the same code
        /// Example result: "Cube mesh action successfully" | "cube mesh shading set to flat successfully"
        /// </summary>
        /// <param name="action">The action performed.</param>
        private void Log(string action) => Debug.Log("<color=gray><b>" + mesh.name + "</b></color> mesh <color=gray><i>" + action + "</i></color> successfully.");

        #region Gizmo

        #region Variable Declarations

        public bool showNormals = false;
        public float normalLength = 0.1f;
        public float normalWidth = 5;
        public Color normalColor = Color.blue;

        public bool showTangents = false;
        public float tangentLength = 0.1f;
        public float tangentWidth = 5;
        public Color tangentColor = Color.red;

        public bool showUV;
        public Color uvSeamColor = Color.green;
        public float uvWidth = 5;

        private FloatField normalsWidthField;
        private FloatField tangentWidthField;
        private FloatField uvWidthField;

        private Label lastDrawnGizmosWith;

        #endregion Variable Declarations

        private void DrawGizmoSettings(VisualElement container)
        {
            NormalsGizmoSettings(container);

            TangentGizmoSettings(container);

            UVGizmoSettings(container);

            CheckeredUVSettings(container);

            var useAntiAliasedGizmosField = container.Q<Toggle>("UseAntiAliasedGizmosField");
            useAntiAliasedGizmosField.value = editorSettings.useAntiAliasedGizmo;
            useAntiAliasedGizmosField.RegisterValueChangedCallback(e =>
            {
                editorSettings.useAntiAliasedGizmo = e.newValue;
                editorSettings.Save();

                SwitchOnOffAAAGizmosFields();
            });

            SwitchOnOffAAAGizmosFields();

            var maximumGizmoDrawTimeField = container.Q<IntegerField>("MaximumGizmoDrawTimeField");
            maximumGizmoDrawTimeField.value = editorSettings.maximumGizmoDrawTime;
            maximumGizmoDrawTimeField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue < 10 && e.newValue > 10000)
                    return;

                editorSettings.maximumGizmoDrawTime = e.newValue;
                editorSettings.Save();
            });

            lastDrawnGizmosWith = container.Q<Label>("LastDrawnGizmosWith");
        }

        private void SwitchOnOffAAAGizmosFields()
        {
            if (editorSettings.useAntiAliasedGizmo)
            {
                normalsWidthField.SetEnabled(true);
                tangentWidthField.SetEnabled(true);
                uvWidthField.SetEnabled(true);
            }
            else
            {
                normalsWidthField.SetEnabled(false);
                tangentWidthField.SetEnabled(false);
                uvWidthField.SetEnabled(false);
            }
        }

        private void NormalsGizmoSettings(VisualElement container)
        {
            Toggle showNormalsField = container.Q<Toggle>("showNormals");
            FloatField normalsLengthField = container.Q<FloatField>("normalLength");
            normalsWidthField = container.Q<FloatField>("normalWidth");
            ColorField normalsColorField = container.Q<ColorField>("normalColor");

            if (!showNormalsField.value) HideNormalsGizmoSettings(normalsLengthField, normalsWidthField, normalsColorField);

            showNormalsField.RegisterValueChangedCallback(ev =>
            {
                showNormals = ev.newValue;

                if (ev.newValue)
                {
                    normalsLengthField.style.display = DisplayStyle.Flex;
                    normalsWidthField.style.display = DisplayStyle.Flex;
                    normalsColorField.style.display = DisplayStyle.Flex;
                }
                else
                {
                    HideNormalsGizmoSettings(normalsLengthField, normalsWidthField, normalsColorField);
                }
                SceneView.RepaintAll();
            });
            normalsLengthField.RegisterValueChangedCallback(ev =>
            {
                normalLength = ev.newValue;
                SceneView.RepaintAll();
            });
            normalsWidthField.RegisterValueChangedCallback(ev =>
            {
                normalWidth = ev.newValue;
                SceneView.RepaintAll();
            });
            normalsColorField.RegisterValueChangedCallback(ev =>
            {
                normalColor = ev.newValue;
                SceneView.RepaintAll();
            });
        }

        private void HideNormalsGizmoSettings(FloatField normalsLengthField, FloatField normalsWidthField, ColorField normalsColorField)
        {
            normalsLengthField.style.display = DisplayStyle.None;
            normalsWidthField.style.display = DisplayStyle.None;
            normalsColorField.style.display = DisplayStyle.None;
        }

        private void CheckeredUVSettings(VisualElement container)
        {
            Button setCheckeredUV = container.Q<Button>("setCheckeredUV");
            setCheckeredUV.clickable = null;
            setCheckeredUV.clicked += () => AssignCheckerMaterial();
            //var setCheckerField = container.Q<Toggle>("setChecker");

            //setCheckerField.RegisterValueChangedCallback(ev =>
            //{
            //    if (ev.newValue)
            //    {
            //        AssignCheckerMaterial();
            //    }
            //    else
            //    {
            //        ResetMaterial();
            //    }
            //});
        }

        private void AssignCheckerMaterial()
        {
            Debug.Log("Assigning CheckerMaterial");
            if (checkerMaterial != null)
                ResetMaterial();
            if (sourceMeshFilter.GetComponent<Renderer>() == null)
            {
                Debug.Log("Please assign a material.");
                return;
            }
            Material originalMaterial = sourceMeshFilter.GetComponent<Renderer>().sharedMaterial;
            if (originalMaterial == null)
            {
                Debug.Log("Please assign a material.");
                return;
            }

            if (checkerMaterial != null)
            {
                DestroyImmediate(checkerMaterial);
            }

            checkerMaterial = new Material(originalMaterial);
            checkerMaterial.name = "Checkered Material";

            int width = root.Q<IntegerField>("UVWidth").value;
            int height = root.Q<IntegerField>("UVHeight").value;
            int cellSize = root.Q<IntegerField>("UVCellSize").value;
            checkerMaterial.mainTexture = CreateCheckeredTexture(width, height, cellSize);

            //editorRepainted = false;
            Undo.RecordObject(sourceMeshFilter.GetComponent<Renderer>(), "UV Material apply");
            sourceMeshFilter.GetComponent<Renderer>().sharedMaterial = checkerMaterial;
            //checkerMaterial.SetTexture("Checkered Texture", null);
        }

        private void UVGizmoSettings(VisualElement container)
        {
            Toggle showUVField = container.Q<Toggle>("showUV");
            uvWidthField = container.Q<FloatField>("uvWidth");
            ColorField uvColorField = container.Q<ColorField>("uvColor");

            if (!showUVField.value) HideUVGizmoSettings(uvWidthField, uvColorField);

            showUVField.RegisterValueChangedCallback(ev =>
            {
                showUV = ev.newValue;

                if (ev.newValue)
                {
                    uvWidthField.style.display = DisplayStyle.Flex;
                    uvColorField.style.display = DisplayStyle.Flex;
                }
                else
                {
                    HideUVGizmoSettings(uvWidthField, uvColorField);
                }
                SceneView.RepaintAll();
            });
            uvWidthField.RegisterValueChangedCallback(ev =>
            {
                uvWidth = ev.newValue;
                SceneView.RepaintAll();
            });
            uvColorField.RegisterValueChangedCallback(ev =>
            {
                uvSeamColor = ev.newValue;
                SceneView.RepaintAll();
            });
        }

        private void HideUVGizmoSettings(FloatField uvWidthField, ColorField uvColorField)
        {
            uvWidthField.style.display = DisplayStyle.None;
            uvColorField.style.display = DisplayStyle.None;
        }

        private void TangentGizmoSettings(VisualElement container)
        {
            Toggle showTangentsField = container.Q<Toggle>("showTangents");
            FloatField tangentLengthField = container.Q<FloatField>("tangentLength");
            tangentWidthField = container.Q<FloatField>("tangentWidth");
            ColorField tangentColorField = container.Q<ColorField>("tangentColor");

            if (!showTangentsField.value) HideTangentGizmoSettings(tangentLengthField, tangentWidthField, tangentColorField);

            showTangentsField.RegisterValueChangedCallback(ev =>
            {
                showTangents = ev.newValue;

                if (ev.newValue)
                {
                    tangentLengthField.style.display = DisplayStyle.Flex;
                    tangentWidthField.style.display = DisplayStyle.Flex;
                    tangentColorField.style.display = DisplayStyle.Flex;
                }
                else
                {
                    HideTangentGizmoSettings(tangentLengthField, tangentWidthField, tangentColorField);
                }
                SceneView.RepaintAll();
            });
            tangentLengthField.RegisterValueChangedCallback(ev =>
            {
                tangentLength = ev.newValue;
                SceneView.RepaintAll();
            });
            tangentWidthField.RegisterValueChangedCallback(ev =>
            {
                tangentWidth = ev.newValue;
                SceneView.RepaintAll();
            });
            tangentColorField.RegisterValueChangedCallback(ev =>
            {
                tangentColor = ev.newValue;
                SceneView.RepaintAll();
            });
        }

        private void HideTangentGizmoSettings(FloatField tangentLengthField, FloatField tangentWidthField, ColorField tangentColorField)
        {
            tangentLengthField.style.display = DisplayStyle.None;
            tangentWidthField.style.display = DisplayStyle.None;
            tangentColorField.style.display = DisplayStyle.None;
        }

        private Stopwatch stopwatch;
        /// <summary>
        /// This can impact performance when there are many vertices in the mesh.
        /// Use this for debugging purposes and remove it or disable it
        /// </summary>

        private void OnSceneGUI()
        {
            if (sourceMeshFilter == null) return;

            BetterMeshSettings betterMeshSettings = BetterMeshSettings.instance;
            if (betterMeshSettings == null) return;

            Mesh mesh = sourceMeshFilter.sharedMesh;
            if (mesh == null) return;

            if (stopwatch == null) stopwatch = new();
            else stopwatch.Reset();

            stopwatch.Start();

            Transform transform = sourceMeshFilter.GetComponent<Transform>();
            Vector3[] vertices = mesh.vertices;

            bool useAntiAliasedHandles = betterMeshSettings.useAntiAliasedGizmo;
            int maximumGizmoTime = betterMeshSettings.maximumGizmoDrawTime;
            int drawnGizmo = 0;

            if (showNormals || showTangents)
            {
                Vector3[] normals = mesh.normals;
                Vector4[] tangents = mesh.tangents;

                Matrix4x4 localToWorld = transform.localToWorldMatrix;
                Matrix4x4 normalMatrix = transform.localToWorldMatrix;

                bool drawNormals = showNormals && normals.Length == vertices.Length;
                bool drawTangents = showTangents && tangents.Length == vertices.Length;

                for (int i = 0; i < vertices.Length; i++)
                {
                    //Vector3 worldVertex = transform.TransformPoint(vertices[i]);
                    Vector3 worldVertex = localToWorld.MultiplyPoint3x4(vertices[i]);

                    if (drawNormals)
                    {
                        //Vector3 worldNormal = transform.TransformDirection(normals[i]);
                        Vector3 worldNormal = normalMatrix.MultiplyVector(normals[i]);
                        Handles.color = normalColor;
                        if (useAntiAliasedHandles)
                            Handles.DrawAAPolyLine(normalWidth, worldVertex, worldVertex + worldNormal * normalLength);
                        else
                            Handles.DrawLine(worldVertex, worldVertex + worldNormal * normalLength);
                    }

                    if (drawTangents)
                    {
                        //Vector3 worldTangent = transform.TransformDirection(new Vector3(tangents[i].x, tangents[i].y, tangents[i].z));
                        Vector3 worldTangent = normalMatrix.MultiplyVector(tangents[i]);
                        Handles.color = tangentColor;
                        if (useAntiAliasedHandles)
                            Handles.DrawAAPolyLine(tangentWidth, worldVertex, worldVertex + worldTangent * tangentLength);
                        else
                            Handles.DrawLine(worldVertex, worldVertex + worldTangent * tangentLength);
                    }

                    if (stopwatch.ElapsedMilliseconds > maximumGizmoTime)
                    {
                        stopwatch.Stop();

                        if (showNormals)
                            drawnGizmo += i + 1;
                        if (showTangents)
                            drawnGizmo += i + 1;

                        MaximumGizmoDrawingTimeReached(drawnGizmo);
                        return;
                    }
                }
                if (showNormals)
                    drawnGizmo += vertices.Length;
                if (showTangents)
                    drawnGizmo += tangents.Length;
            }

            if (showUV)
            {
                int[] triangles = mesh.triangles;

                Vector2[] uvs = mesh.uv;
                if (uvs.Length == 0) return;

                Handles.color = uvSeamColor;

                Matrix4x4 localToWorld = transform.localToWorldMatrix;

                float threshold = 0.5f * 0.5f; // Compare squared distances to avoid sqrt calculations

                int triangleCount = triangles.Length;

                for (int i = 0; i < triangleCount; i += 3)
                {
                    int indexA = triangles[i];
                    int indexB = triangles[i + 1];
                    int indexC = triangles[i + 2];

                    Vector2 uvA = uvs[indexA];
                    Vector2 uvB = uvs[indexB];
                    Vector2 uvC = uvs[indexC];

                    //if (Vector2.Distance(uvA, uvB) > 0.5f ||
                    //    Vector2.Distance(uvB, uvC) > 0.5f ||
                    //    Vector2.Distance(uvC, uvA) > 0.5f)
                    if ((uvA - uvB).sqrMagnitude > threshold ||
                        (uvB - uvC).sqrMagnitude > threshold ||
                        (uvC - uvA).sqrMagnitude > threshold)
                    {
                        //Vector3 worldVertexA = transform.TransformPoint(vertices[triangles[i]]);
                        //Vector3 worldVertexB = transform.TransformPoint(vertices[triangles[i + 1]]);
                        //Vector3 worldVertexC = transform.TransformPoint(vertices[triangles[i + 2]]);

                        Vector3 worldVertexA = localToWorld.MultiplyPoint3x4(vertices[indexA]);
                        Vector3 worldVertexB = localToWorld.MultiplyPoint3x4(vertices[indexB]);
                        Vector3 worldVertexC = localToWorld.MultiplyPoint3x4(vertices[indexC]);

                        if (useAntiAliasedHandles)
                        {
                            Handles.DrawAAPolyLine(uvWidth, worldVertexA, worldVertexB);
                            Handles.DrawAAPolyLine(uvWidth, worldVertexB, worldVertexC);
                            Handles.DrawAAPolyLine(uvWidth, worldVertexC, worldVertexA);
                        }
                        else
                        {
                            Handles.DrawLine(worldVertexA, worldVertexB);
                            Handles.DrawLine(worldVertexB, worldVertexC);
                            Handles.DrawLine(worldVertexC, worldVertexA);
                        }

                        drawnGizmo += 3;

                        if (stopwatch.ElapsedMilliseconds > maximumGizmoTime)
                        {
                            stopwatch.Stop();
                            MaximumGizmoDrawingTimeReached(drawnGizmo);
                            return;
                        }
                    }
                }
            }

            stopwatch.Stop();
            GizmosDrawingDone(drawnGizmo, stopwatch.ElapsedMilliseconds);
        }

        private void GizmosDrawingDone(int gizmosDrawn, long time)
        {
            if (lastDrawnGizmosWith == null)
                return;

            lastDrawnGizmosWith.text = "Drew " + gizmosDrawn + " handles and it took " + time + "ms.";
        }

        private void MaximumGizmoDrawingTimeReached(int gizmosDrawn)
        {
            if (editorSettings == null)
                return;

            if (lastDrawnGizmosWith == null)
                return;

            lastDrawnGizmosWith.text = "Gizmo drawing stopped after reaching maximum draw time. \nWas able to draw " + gizmosDrawn + " handles.";

            if (editorSettings.useAntiAliasedGizmo)
                lastDrawnGizmosWith.text += "\nTurning off anti aliasing gizmos will help the performance";
        }

        #endregion Gizmo
    }
}