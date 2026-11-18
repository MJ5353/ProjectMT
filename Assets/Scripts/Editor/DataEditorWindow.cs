using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class DataEditorWindow : EditorWindow
{
    private const string StylePath = "Assets/Scripts/Editor/DataEditorWindow.uss";

    [SerializeField] private string m_TypeName = nameof(MonsterDef);
    [SerializeField] private string m_Search = string.Empty;
    [SerializeField] private DataDef m_SelectedDef = null;

    private readonly List<DataDef> m_ListDef = new();
    private readonly List<DataDef> m_ListVisible = new();
    private readonly Dictionary<DataDef, SerializedObject> m_DicSerializedDef = new();
    private List<Type> m_ListType = new();
    private Type m_Type;
    private ListView m_TypeList;
    private MultiColumnListView m_Table;
    private Label m_Title;
    private Label m_Count;
    private Label m_Status;
    private Label m_Empty;
    private ScrollView m_Details;
    private VisualElement m_Related;
    private VisualElement m_Models;
    private VisualElement m_Workspace;
    private Button m_BuildSelected;
    private Button m_BuildAll;
    private Button m_DeleteSelected;
    private ToolbarMenu m_AutoFill;
    private Button m_BuildPickup;
    private ToolbarMenu m_SpawnFill;
    private bool m_ReloadQueued;

    // ------------------------------------------------

    private void OnEnable()
    {
        EditorApplication.projectChanged += QueueReload;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= QueueReload;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Undo.undoRedoPerformed -= OnUndoRedo;
        EditorApplication.delayCall -= Reload;
        ReleaseBindings();
    }

    public void CreateGUI()
    {
        rootVisualElement.UnregisterCallback<SerializedPropertyChangeEvent>(OnPropertyChanged);
        rootVisualElement.Clear();
        rootVisualElement.AddToClassList("data-editor");
        rootVisualElement.EnableInClassList("light", EditorGUIUtility.isProSkin == false);
        StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
        if (style != null)
            rootVisualElement.styleSheets.Add(style);

        Toolbar toolbar = new Toolbar();
        toolbar.Add(new Label("MushTaker  /  Data") { name = "brand" });
        toolbar.Add(new ToolbarSpacer { flex = true });
        toolbar.Add(new ToolbarButton(() => RunAction(() => SetStatus(DataTools.ValidateAll() ? "데이터 검사 통과" : "데이터 오류를 Console에서 확인하세요."))) { text = "데이터 검사" });
        toolbar.Add(new ToolbarButton(() => RunAction(() => { DataTools.RefreshDatabase(true); SetStatus("데이터베이스 갱신 완료"); })) { text = "DB 갱신" });
        toolbar.Add(new ToolbarButton(Reload) { text = "새로고침" });
        toolbar.Add(new ToolbarButton(Save) { text = "변경 저장", tooltip = "이 창에 불러온 Def만 저장합니다." });
        rootVisualElement.Add(toolbar);

        m_Workspace = new VisualElement { name = "workspace" };
        TwoPaneSplitView typesSplit = new TwoPaneSplitView(0, 160f, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "data-types-split" };
        VisualElement sidebar = new VisualElement { name = "sidebar" };
        sidebar.Add(new Label("데이터 타입") { name = "sidebar-title" });
        m_TypeList = new ListView { fixedItemHeight = 46f, selectionType = SelectionType.Single, showBorder = false };
        m_TypeList.makeItem = () =>
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("type-row");
            row.Add(new Label { name = "type-label" });
            row.Add(new Label { name = "type-count" });
            return row;
        };
        m_TypeList.bindItem = (element, index) =>
        {
            Type type = m_ListType[index];
            element.Q<Label>("type-label").text = DataEditorUtility.GetTypeLabel(type);
            element.Q<Label>("type-count").text = type.Name + "  ·  " + m_ListDef.Count(def => def.GetType() == type);
        };
        m_TypeList.selectionChanged += items =>
        {
            Type selected = items.OfType<Type>().FirstOrDefault();
            if (selected != null)
                SelectType(selected);
        };
        sidebar.Add(m_TypeList);
        typesSplit.Add(sidebar);

        TwoPaneSplitView detailSplit = new TwoPaneSplitView(1, 370f, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "data-detail-split" };
        VisualElement center = new VisualElement { name = "center" };
        VisualElement heading = new VisualElement { name = "heading" };
        m_Title = new Label { name = "type-title" };
        m_Count = new Label { name = "type-subtitle" };
        heading.Add(m_Title);
        heading.Add(m_Count);
        center.Add(heading);

        VisualElement actions = new VisualElement { name = "actions" };
        ToolbarSearchField search = new ToolbarSearchField { value = m_Search, name = "search", tooltip = "이름 · ID · 에셋 이름 검색" };
        search.RegisterValueChangedCallback(evt => { m_Search = evt.newValue; FilterRows(); RefreshModels(); });
        actions.Add(search);
        actions.Add(new Button(CreateDef) { text = "+ 새 Def" });
        m_DeleteSelected = new Button(DeleteSelectedDefs) { name = "delete-defs", text = "선택 삭제", tooltip = "선택한 Def 에셋만 휴지통으로 이동합니다. 모델·프리팹은 유지합니다." };
        actions.Add(m_DeleteSelected);
        m_BuildSelected = new Button(() => BuildMonsters(false)) { text = "선택 빌드" };
        m_BuildAll = new Button(() => BuildMonsters(true)) { text = "타입 전체 빌드" };
        actions.Add(m_BuildSelected);
        actions.Add(m_BuildAll);
        m_AutoFill = new ToolbarMenu { text = "자동 배정", tooltip = "공식 밸런스, 지역 버섯 드랍, 소켓 버섯을 채웁니다. 적용 전에 미리 보여 줍니다." };
        foreach (bool isAll in new[] { false, true })
        {
            string scope = isAll ? "타입 전체" : "선택한 몬스터";
            m_AutoFill.menu.AppendAction($"{scope}/전부", _ => AutoFillMonsters(MonsterAutoFill.EPart.All, isAll));
            m_AutoFill.menu.AppendAction($"{scope}/밸런스만", _ => AutoFillMonsters(MonsterAutoFill.EPart.Balance, isAll));
            m_AutoFill.menu.AppendAction($"{scope}/드랍만", _ => AutoFillMonsters(MonsterAutoFill.EPart.Drop, isAll));
            m_AutoFill.menu.AppendAction($"{scope}/버섯 꽂기만 (다시 굴리기)", _ => AutoFillMonsters(MonsterAutoFill.EPart.Socket, isAll));
            m_AutoFill.menu.AppendAction($"{scope}/보스 몸 판정", _ => AutoFillBodyShapes(isAll));
        }
        m_AutoFill.menu.AppendSeparator();
        m_AutoFill.menu.AppendAction("밸런스 공식 열기", _ => RunAction(() => Selection.activeObject = MonsterAutoFill.LoadTable()));
        actions.Add(m_AutoFill);
        m_BuildPickup = new Button(() => BuildPickups(false)) { text = "픽업 전체 빌드", tooltip = "버섯 Def마다 Pickup_NNN_Key 프리팹을 만들고 Def에 연결합니다. 이미 있는 픽업은 ID·연결만 맞춥니다." };
        actions.Add(m_BuildPickup);
        m_SpawnFill = new ToolbarMenu { text = "스폰 자동 배정", tooltip = "맵 FBX의 SpawnSlot에 지역 몬스터를 채웁니다. 적용 전에 미리 보여 줍니다." };
        m_SpawnFill.menu.AppendAction("선택한 맵", _ => AutoFillSpawns(false));
        m_SpawnFill.menu.AppendAction("타입 전체", _ => AutoFillSpawns(true));
        actions.Add(m_SpawnFill);
        center.Add(actions);

        m_Table = new MultiColumnListView
        {
            fixedItemHeight = 30f,
            selectionType = SelectionType.Multiple,
            showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            sortingMode = ColumnSortingMode.Default,
            viewDataKey = "data-table",
        };
        m_Table.selectionChanged += items => ShowDetails(items.OfType<DataDef>().FirstOrDefault());
        m_Table.style.flexGrow = 1f;
        m_Table.RegisterCallback<GeometryChangedEvent>(evt => FitColumns(evt.newRect.width));
        center.Add(m_Table);
        m_Empty = new Label("표시할 데이터가 없습니다.") { name = "empty" };
        center.Add(m_Empty);
        m_Models = new VisualElement { name = "models" };
        center.Add(m_Models);
        detailSplit.Add(center);

        m_Details = new ScrollView { name = "details", viewDataKey = "data-details" };
        detailSplit.Add(m_Details);
        typesSplit.Add(detailSplit);
        m_Workspace.Add(typesSplit);
        rootVisualElement.Add(m_Workspace);
        m_Status = new Label("수치는 표에서, 설명·공격·목록은 상세 영역에서 편집합니다.  Ctrl+Z / Ctrl+Y") { name = "status" };
        rootVisualElement.Add(m_Status);
        rootVisualElement.RegisterCallback<SerializedPropertyChangeEvent>(OnPropertyChanged);
        Reload();
        OnPlayModeChanged(PlayModeStateChange.EnteredEditMode);
    }

    // ------------------------------------------------

    [MenuItem("MushTaker/데이터 편집기", false, 0)]
    public static void Open()
    {
        bool isOpen = HasOpenInstances<DataEditorWindow>();
        DataEditorWindow window = GetWindow<DataEditorWindow>("데이터 편집기");
        window.minSize = new Vector2(850f, 420f);
        if (isOpen == false)
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            float width = Mathf.Min(1400f, main.width - 40f);
            float height = Mathf.Min(780f, main.height - 80f);
            window.position = new Rect(main.center.x - width * 0.5f, main.center.y - height * 0.5f, width, height);
        }
    }

    private void QueueReload()
    {
        if (m_ReloadQueued)
            return;

        m_ReloadQueued = true;
        EditorApplication.delayCall += Reload;
    }

    private void Reload()
    {
        m_ReloadQueued = false;
        EditorApplication.delayCall -= Reload;
        if (m_Table == null)
            return;

        DataDef selected = m_SelectedDef;
        ReleaseBindings();
        m_ListDef.Clear();
        m_ListDef.AddRange(DataTools.CollectDefs());
        foreach (DataDef def in m_ListDef)
        {
            m_DicSerializedDef.Add(def, new SerializedObject(def));
        }

        m_ListType = DataEditorUtility.CollectTypes();
        m_TypeList.itemsSource = m_ListType;
        m_TypeList.Rebuild();
        Type type = m_ListType.FirstOrDefault(candidate => candidate.Name == m_TypeName) ?? m_ListType.FirstOrDefault();
        if (type == null)
            return;

        m_TypeList.SetSelectionWithoutNotify(new[] { m_ListType.IndexOf(type) });
        m_SelectedDef = selected;
        SelectType(type);
    }

    private void ReleaseBindings()
    {
        rootVisualElement.Unbind();
        if (m_Table != null)
            m_Table.itemsSource = null;
        m_Table?.columns.Clear();
        foreach (var pair in m_DicSerializedDef)
        {
            pair.Value.Dispose();
        }

        m_DicSerializedDef.Clear();
    }

    private void SelectType(Type type)
    {
        DataDef selected = m_SelectedDef != null && m_SelectedDef.GetType() == type ? m_SelectedDef : null;
        m_Type = type;
        m_TypeName = type.Name;
        m_Title.text = DataEditorUtility.GetTypeLabel(type);
        m_Table.itemsSource = null;
        m_SelectedDef = selected;
        m_Table.columns.Clear();
        AddLabelColumn("ID", 145f, def => def.ID);
        AddPropertyColumn("m_DisplayName", "이름", 130f, true);
        if (typeof(UnitDef).IsAssignableFrom(type))
            AddPropertyColumn(nameof(UnitDef.MaxHp), "체력", 60f);
        if (type == typeof(MonsterDef) || type == typeof(PlayerDef))
            AddPropertyColumn("MoveSpeed", "이동", 65f);
        if (type == typeof(MonsterDef))
        {
            AddPropertyColumn(nameof(MonsterDef.SightRange), "시야", 65f);
            AddLabelColumn("프리팹", 90f, def => ((MonsterDef)def).Prefab != null ? "연결됨" : "빌드 필요");
        }
        else if (type == typeof(PlayerDef))
            AddPropertyColumn(nameof(PlayerDef.DashCooldown), "회피 쿨타임", 90f);
        else if (type == typeof(MushroomDef))
        {
            AddPropertyColumn(nameof(MushroomDef.DexNo), "도감 번호", 75f);
            AddPropertyColumn(nameof(MushroomDef.Rarity), "등급", 100f);
            AddPropertyColumn(nameof(MushroomDef.Tint), "색상", 100f);
            AddLabelColumn("픽업", 80f, def => ((MushroomDef)def).Pickup != null ? "연결됨" : "빌드 필요");
        }
        else if (type == typeof(MapDef))
        {
            AddPropertyColumn(nameof(MapDef.Order), "순서", 60f);
            AddPropertyColumn(nameof(MapDef.PlayerSlot), "시작 슬롯", 75f);
            AddLabelColumn("스폰", 65f, def => ((MapDef)def).ListSpawn.Count.ToString());
        }
        m_BuildSelected.style.display = type == typeof(MonsterDef) ? DisplayStyle.Flex : DisplayStyle.None;
        m_BuildAll.style.display = m_BuildSelected.style.display;
        m_AutoFill.style.display = m_BuildSelected.style.display;
        m_BuildPickup.style.display = type == typeof(MushroomDef) ? DisplayStyle.Flex : DisplayStyle.None;
        m_SpawnFill.style.display = type == typeof(MapDef) ? DisplayStyle.Flex : DisplayStyle.None;
        FilterRows();
        RefreshModels();
    }

    private void AddLabelColumn(string title, float width, Func<DataDef, string> value)
    {
        m_Table.columns.Add(new Column
        {
            name = title, title = title, width = width, minWidth = 55f, stretchable = false, sortable = true, resizable = true,
            makeCell = () => { Label label = new Label(); label.AddToClassList("text-cell"); return label; },
            bindCell = (element, index) => { Label label = (Label)element; label.text = value(m_ListVisible[index]); label.tooltip = label.text; },
            comparison = (left, right) => string.Compare(value(m_ListVisible[left]), value(m_ListVisible[right]), StringComparison.Ordinal),
        });
    }

    private void AddPropertyColumn(string field, string title, float width, bool stretch = false)
    {
        m_Table.columns.Add(new Column
        {
            name = field, title = title, width = width, minWidth = stretch ? 90f : 50f, stretchable = stretch, sortable = true, resizable = true,
            makeCell = () => { PropertyField property = new PropertyField { label = string.Empty }; property.AddToClassList("data-cell"); return property; },
            bindCell = (element, index) =>
            {
                PropertyField cell = (PropertyField)element;
                cell.Unbind();
                cell.BindProperty(DataEditorUtility.FindProperty(m_DicSerializedDef[m_ListVisible[index]], field));
            },
            unbindCell = (element, index) => element.Unbind(),
            comparison = (left, right) => CompareField(m_ListVisible[left], m_ListVisible[right], field),
        });
    }

    private int CompareField(DataDef left, DataDef right, string field)
    {
        SerializedProperty a = DataEditorUtility.FindProperty(m_DicSerializedDef[left], field);
        SerializedProperty b = DataEditorUtility.FindProperty(m_DicSerializedDef[right], field);
        if (a.propertyType == SerializedPropertyType.Integer || a.propertyType == SerializedPropertyType.Enum)
            return a.intValue.CompareTo(b.intValue);
        if (a.propertyType == SerializedPropertyType.Float)
            return a.doubleValue.CompareTo(b.doubleValue);

        return string.Compare(a.propertyType == SerializedPropertyType.String ? a.stringValue : left.ID,
            b.propertyType == SerializedPropertyType.String ? b.stringValue : right.ID, StringComparison.Ordinal);
    }

    private void FilterRows()
    {
        if (m_Type == null)
            return;

        DataDef selected = m_SelectedDef;
        m_Table.ClearSelection();
        m_ListVisible.Clear();
        m_ListVisible.AddRange(m_ListDef.Where(def => def.GetType() == m_Type && DataEditorUtility.MatchesSearch(def, m_Search)));
        m_Table.itemsSource = m_ListVisible;
        m_Table.Rebuild();
        m_Count.text = $"{m_Type.Name}  ·  {m_ListVisible.Count} / {m_ListDef.Count(def => def.GetType() == m_Type)}개";
        m_Empty.style.display = m_ListVisible.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (selected != null && m_ListVisible.Contains(selected))
            m_Table.SetSelection(m_ListVisible.IndexOf(selected));
        else if (m_ListVisible.Count > 0)
            m_Table.SetSelection(0);
        else
            ShowDetails(null);
    }

    private void FitColumns(float width)
    {
        float remaining = width - 250f;
        foreach (Column column in m_Table.columns)
        {
            if (column.name == "ID" || column.name == "m_DisplayName")
                continue;

            float size = column.width.value + 4f;
            column.visible = remaining >= size;
            if (column.visible)
                remaining -= size;
        }
    }

    private void ShowDetails(DataDef def)
    {
        m_SelectedDef = def;
        m_Details.Unbind();
        m_Details.Clear();
        m_BuildSelected.SetEnabled(m_Table.selectedItems.OfType<MonsterDef>().Any());
        m_DeleteSelected.SetEnabled(m_Table.selectedItems.OfType<DataDef>().Any());
        if (def == null)
        {
            m_Details.Add(new Label("상세 편집") { name = "detail-title" });
            m_Details.Add(new Label("테이블에서 항목을 선택하세요.") { name = "detail-hint" });
            return;
        }

        m_Details.Add(new Label(def.DisplayName) { name = "detail-title" });
        m_Details.Add(new Label(def.GetType().Name + "  /  " + def.ID) { name = "detail-hint" });
        Button ping = new Button(() => EditorGUIUtility.PingObject(def)) { text = "프로젝트에서 찾기", tooltip = AssetDatabase.GetAssetPath(def) };
        m_Details.Add(ping);
        m_Related = new VisualElement { name = "related" };
        m_Details.Add(m_Related);
        RefreshRelations();
        m_Details.Add(new Label("속성") { name = "properties-title" });
        AddProperties(m_Details, m_DicSerializedDef[def], false);
    }

    private static void AddProperties(VisualElement parent, SerializedObject serializedDef, bool readOnly)
    {
        SerializedProperty property = serializedDef.GetIterator();
        bool enter = true;
        while (property.NextVisible(enter))
        {
            enter = false;
            if (property.name == "m_Script")
                continue;

            if (readOnly && (property.propertyType == SerializedPropertyType.Generic || property.isArray && property.propertyType != SerializedPropertyType.String))
            {
                AddReadOnlyGroup(parent, property.Copy());
                continue;
            }

            PropertyField field = new PropertyField(property.Copy());
            field.AddToClassList("detail-field");
            field.SetEnabled(readOnly == false && property.name != "<ID>k__BackingField");
            parent.Add(field);
            field.BindProperty(property.Copy());
        }
    }

    private static void AddReadOnlyGroup(VisualElement parent, SerializedProperty property)
    {
        Foldout foldout = new Foldout { text = property.displayName, value = false };
        parent.Add(foldout);
        SerializedProperty child = property.Copy();
        SerializedProperty end = property.GetEndProperty();
        bool enter = true;
        while (child.NextVisible(enter) && SerializedProperty.EqualContents(child, end) == false)
        {
            enter = false;
            if (child.propertyType == SerializedPropertyType.Generic || child.isArray && child.propertyType != SerializedPropertyType.String)
                AddReadOnlyGroup(foldout, child.Copy());
            else
            {
                PropertyField field = new PropertyField(child.Copy());
                field.SetEnabled(false);
                foldout.Add(field);
                field.BindProperty(child.Copy());
            }
        }
    }

    private void RefreshRelations()
    {
        if (m_SelectedDef == null || m_Related == null)
            return;

        m_Related.Unbind();
        m_Related.Clear();
        List<DataDef> listRelated = DataEditorUtility.FindRelatedDefs(m_SelectedDef, m_ListDef);
        Foldout group = new Foldout { text = $"연관 Def  ·  {listRelated.Count}", value = true };
        if (listRelated.Count == 0)
            group.Add(new Label("연결된 Def가 없습니다.") { name = "related-empty" });
        foreach (DataDef related in listRelated)
        {
            Foldout card = new Foldout { text = related.DisplayName + "  ·  " + DataEditorUtility.GetTypeLabel(related.GetType()), value = false };
            card.AddToClassList("related-card");
            // 읽기 전용으로 펼쳐 현재 선택을 유지한 채 확인한다.
            if (m_DicSerializedDef.TryGetValue(related, out SerializedObject serializedRelated) == false)
            {
                serializedRelated = new SerializedObject(related);
                m_DicSerializedDef.Add(related, serializedRelated);
            }
            AddProperties(card, serializedRelated, true);
            group.Add(card);
        }
        m_Related.Add(group);
    }

    private void RefreshModels()
    {
        m_Models.Clear();
        if (m_Type != typeof(MonsterDef) && m_Type != typeof(MushroomDef))
            return;

        List<GameObject> listAvailable = m_Type == typeof(MonsterDef)
            ? MonsterPrefabBuilder.FindUnregisteredModels(m_ListDef.OfType<MonsterDef>())
            : MushroomDefBuilder.FindUnregisteredModels(m_ListDef.OfType<MushroomDef>());
        List<GameObject> listModel = listAvailable
            .Where(model => string.IsNullOrWhiteSpace(m_Search) || model.name.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        Foldout section = new Foldout { text = $"미등록 모델  ·  {listModel.Count}", value = true };
        string hint = m_Type == typeof(MonsterDef) ? "Def를 추가한 뒤 수치를 설정하고 프리팹을 빌드하세요."
            : "도감 번호로 기존 Def와 대조합니다. 파손 모델은 제외하며, 추가 시 모델 번호를 채웁니다.";
        section.Add(new Label(hint) { name = "model-hint" });
        if (listModel.Count == 0)
            section.Add(new Label(string.IsNullOrWhiteSpace(m_Search) ? "모든 등록 대상 모델에 Def가 있습니다." : "검색 조건에 맞는 미등록 모델이 없습니다.") { name = "model-hint" });
        else
        {
            Button addAll = new Button(() => RegisterModels(listModel)) { text = $"미등록 {listModel.Count}개 Def 추가" };
            section.Add(addAll);
            ListView models = new ListView(listModel, 30f,
                () => { VisualElement row = new VisualElement(); row.AddToClassList("model-row"); row.Add(new Label()); row.Add(new Button { text = "Def 추가" }); return row; },
                (element, index) =>
                {
                    GameObject model = listModel[index];
                    element.Q<Label>().text = model.name;
                    element.Q<Label>().tooltip = AssetDatabase.GetAssetPath(model);
                    element.Q<Button>().clickable = new Clickable(() => RegisterModels(new[] { model }));
                });
            models.selectionType = SelectionType.None;
            models.style.height = Mathf.Min(150f, listModel.Count * 30f);
            section.Add(models);
        }
        m_Models.Add(section);
    }

    private void RegisterModels(IEnumerable<GameObject> models)
    {
        RunAction(() =>
        {
            int count = 0;
            foreach (GameObject model in models)
            {
                DataDef def = m_Type == typeof(MonsterDef) ? MonsterPrefabBuilder.CreateDef(model) : MushroomDefBuilder.CreateDef(model);
                AssetDatabase.SaveAssetIfDirty(def);
                m_SelectedDef = def;
                count++;
            }
            DataTools.RefreshDatabase(false);
            Reload();
            SetStatus(m_Type == typeof(MonsterDef) ? $"모델에서 Def {count}개 등록 완료. 수치 확인 후 빌드하세요." : $"버섯 Def {count}개 등록 완료.");
        });
    }

    private void CreateDef()
    {
        string path = EditorUtility.SaveFilePanelInProject("새 " + m_Type.Name, m_Type.Name.Replace("Def", "") + "_New", "asset", "저장 위치를 선택하세요.", "Assets/Resources/Data");
        if (string.IsNullOrEmpty(path))
            return;

        RunAction(() =>
        {
            m_SelectedDef = DataEditorUtility.CreateDef(m_Type, path);
            DataTools.RefreshDatabase(false);
            Reload();
            SetStatus("새 Def 생성 완료");
        });
    }

    private void BuildMonsters(bool all)
    {
        List<MonsterDef> listDef = (all ? m_ListDef.Where(def => def.GetType() == m_Type) : m_Table.selectedItems.OfType<DataDef>()).OfType<MonsterDef>().ToList();
        if (listDef.Count == 0)
            return;
        if (listDef.Any(def => System.IO.File.Exists(MonsterPrefabBuilder.GetPrefabPath(def)))
            && EditorUtility.DisplayDialog("몬스터 프리팹 빌드", $"{listDef.Count}개 Def의 프리팹을 빌드합니다. 기존 프리팹과 애니메이터의 수동 수정은 덮어씁니다.", "빌드", "취소") == false)
            return;

        RunAction(() =>
        {
            int built = MonsterPrefabBuilder.Build(listDef, true);
            foreach (MonsterDef def in listDef)
            {
                AssetDatabase.SaveAssetIfDirty(def);
            }
            Reload();
            SetStatus($"프리팹 {built} / {listDef.Count}개 빌드. 실패 항목은 Console에서 확인하세요.");
        });
    }

    private void AutoFillMonsters(MonsterAutoFill.EPart parts, bool isAll)
    {
        List<MonsterDef> listDef = isAll ? m_ListDef.OfType<MonsterDef>().ToList() : m_Table.selectedItems.OfType<MonsterDef>().ToList();
        if (listDef.Count == 0)
            return;

        RunAction(() =>
        {
            if (MonsterAutoFill.Run(listDef, parts) == false)
                return;

            Reload();
            SetStatus($"몬스터 {listDef.Count}개 자동 배정({MonsterAutoFill.GetPartLabel(parts)}) 완료. 경고는 Console에서 확인하세요.");
        });
    }

    // 보스만 Hitbox 뼈마다 구를 채운다(BodyShapeAutoFill)
    private void AutoFillBodyShapes(bool isAll)
    {
        List<MonsterDef> listDef = isAll ? m_ListDef.OfType<MonsterDef>().ToList() : m_Table.selectedItems.OfType<MonsterDef>().ToList();
        if (listDef.Count == 0)
            return;

        RunAction(() =>
        {
            if (BodyShapeAutoFill.Run(listDef) == false)
                return;

            Reload();
            SetStatus("보스 몸 판정 자동 채움 완료. 씬에서 몬스터를 골라 기즈모로 확인하세요.");
        });
    }

    private void AutoFillSpawns(bool isAll)
    {
        List<MapDef> listMap = isAll ? m_ListDef.OfType<MapDef>().ToList() : m_Table.selectedItems.OfType<MapDef>().ToList();
        if (listMap.Count == 0)
            return;

        RunAction(() =>
        {
            if (MapSpawnAutoFill.Run(listMap) == false)
                return;

            Reload();
            SetStatus($"맵 {listMap.Count}개 스폰 자동 배정 완료. 경고는 Console에서 확인하세요.");
        });
    }

    private void BuildPickups(bool isForce)
    {
        List<MushroomDef> listDef = m_ListDef.OfType<MushroomDef>().ToList();

        RunAction(() =>
        {
            int built = MushroomPickupBuilder.Build(listDef, isForce);
            Reload();
            SetStatus($"픽업 {built}개 제작, 버섯 {listDef.Count}개 연결 확인. 실패 항목은 Console에서 확인하세요.");
        });
    }

    private void DeleteSelectedDefs()
    {
        List<DataDef> listDef = m_Table.selectedItems.OfType<DataDef>().ToList();
        if (listDef.Count == 0)
            return;

        RunAction(() =>
        {
            string[] paths = DataEditorUtility.GetDeletePaths(listDef);
            List<string> listReference = DataEditorUtility.FindDeleteReferences(listDef);
            string targets = string.Join("\n", paths.Take(10));
            if (paths.Length > 10)
                targets += $"\n외 {paths.Length - 10}개";

            string references = listReference.Count == 0 ? "확인된 사용 참조 없음" : $"사용 참조 {listReference.Count}개:\n" + string.Join("\n", listReference.Take(8));
            if (listReference.Count > 8)
                references += $"\n외 {listReference.Count - 8}개";

            string message = $"Def {paths.Length}개를 휴지통으로 이동합니다.\n\n{targets}\n\n{references}\n\n참조 검사는 저장된 에셋과 현재 Def 기준입니다. 참조가 끊길 수 있으며, 문자열 ID와 저장 데이터는 자동 수정하지 않습니다.\nFBX·프리팹은 유지합니다. 삭제는 Ctrl+Z로 복구되지 않습니다.";
            if (EditorUtility.DisplayDialog("선택 Def 삭제", message, "휴지통으로 이동", "취소") == false)
                return;

            ReleaseBindings();
            try
            {
                List<string> listFailed = DataEditorUtility.DeleteDefs(listDef);
                if (listFailed.Count > 0)
                    throw new System.IO.IOException($"Def {paths.Length - listFailed.Count}개 삭제, {listFailed.Count}개 실패:\n" + string.Join("\n", listFailed));
            }
            finally
            {
                m_SelectedDef = null;
                try
                {
                    DataTools.RefreshDatabase(false);
                }
                finally
                {
                    Reload();
                }
            }
            SetStatus($"Def {paths.Length}개를 휴지통으로 이동했습니다.");
        });
    }

    private void Save()
    {
        RunAction(() =>
        {
            foreach (DataDef def in m_ListDef)
            {
                AssetDatabase.SaveAssetIfDirty(def);
            }
            SetStatus("Def 변경 저장 완료");
        });
    }

    private void OnPropertyChanged(SerializedPropertyChangeEvent evt)
    {
        if (EditorUtility.IsDirty(evt.changedProperty.serializedObject.targetObject) == false)
            return;

        SetStatus("미저장 변경이 있습니다.  Ctrl+Z / Ctrl+Y  ·  변경 저장");
        if (evt.changedProperty.propertyType == SerializedPropertyType.ObjectReference || evt.changedProperty.propertyType == SerializedPropertyType.Generic)
            m_Related?.schedule.Execute(RefreshRelations);
    }

    private void OnUndoRedo()
    {
        m_Table?.RefreshItems();
        RefreshRelations();
        SetStatus("Undo / Redo 적용");
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        rootVisualElement.SetEnabled(EditorApplication.isPlayingOrWillChangePlaymode == false);
    }

    private void RunAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message);
            Debug.LogException(exception);
        }
    }

    private void SetStatus(string message)
    {
        if (m_Status != null)
            m_Status.text = message;
    }
}
