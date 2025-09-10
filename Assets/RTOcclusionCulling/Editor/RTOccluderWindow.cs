//using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class RTOccluderWindow : EditorWindow
{
    [MenuItem("Window/RTOcclusionCulling/Open Workspace")]
    static void Init()
    {
        RTOccluderWindow window = (RTOccluderWindow)EditorWindow.GetWindow(typeof(RTOccluderWindow), false, "RTWorkspace");
        window.Show();
    }

    #region ToolAsset
    static class ToolAsset
    {
        static public GUIStyle select;
        static public GUIStyle blank;
        static public GUIStyle select_defocus;

        static GUIStyle BackgroundColor(Color c)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixels(new Color[] { c });
            tex.Apply();
            GUIStyle s = new GUIStyle();
            s.normal.background = tex;
            return s;
        }

        static public void Init(string editorpath)
        {
            if (select == null || select.normal.background == null)
            {
                select = BackgroundColor(new Color32(62, 95, 150, 255));
                blank = BackgroundColor(new Color32(0, 0, 0, 0));
                select_defocus = BackgroundColor(new Color32(72, 72, 72, 255));
            }
        }
        static public GUIStyle FindCustomStyle(string name)
        {
            for (int i = 0; i < GUI.skin.customStyles.Length; i++)
                if (GUI.skin.customStyles[i].name == name)
                    return GUI.skin.customStyles[i];
            return null;
        }
    };
    #endregion

    void OnEnable()
    {
        string editorpath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
        ToolAsset.Init(editorpath);

        m_ListView = new ListView();
        m_ListView.Clear(this);
    }

    bool m_Focus = false;

    void OnFocus()
    {
        string editorpath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
        ToolAsset.Init(editorpath);

        m_Focus = true;
    }

    void OnLostFocus()
    {
        m_Focus = false;
    }

    ListView m_ListView;

    List<GameObject> m_ObjectList = new List<GameObject>();
    List<RTOccludee> m_Ocludee = new List<RTOccludee>();
    List<MeshRenderer> m_MeshRenderers = new List<MeshRenderer>();
    Dictionary<Renderer, RTOccludee> m_ObjectOccludee = new Dictionary<Renderer, RTOccludee>();

    public enum Filter
    {
        Occluder,
        Occludee,
        MeshRenderers,
        SelectedMeshRenderer
    };

    Filter m_Filter;
    Filter m_SelectFilter = Filter.MeshRenderers;

    void UpdateItems(Filter filter)
    {
        switch (filter)
        {
            case Filter.Occluder:
            {
                m_ListView.onDrawItemDelegate = delegate (int idx)
                {
                    GUILayout.Label(m_ObjectList[idx] == null ? "(deleted)" : m_ObjectList[idx].name);
                };
                m_ListView.onUpdateSelection = delegate ()
                {
                    int[] sel = m_ListView.Selection;
                    List<GameObject> list = new List<GameObject>();
                    for (int i = 0; i < sel.Length; i++)
                        list.Add(m_ObjectList[sel[i]]);
                    Selection.objects = list.ToArray();
                };

                m_ObjectList.Clear();
                RTConvexTowerOccluder[] script3 = GameObject.FindObjectsOfType<RTConvexTowerOccluder>();
                for (int i = 0; i < script3.Length; i++)
                    m_ObjectList.Add(script3[i].gameObject);
                RTSimpleOccluder[] script4 = GameObject.FindObjectsOfType<RTSimpleOccluder>();
                for (int i = 0; i < script4.Length; i++)
                    m_ObjectList.Add(script4[i].gameObject);

                m_ListView.Clear(this);
                m_ListView.SeCount(m_ObjectList.Count);

                GameObject[] current = Selection.gameObjects;
                List<int> selobjs = new List<int>();
                for (int i = 0; i < current.Length; i++)
                {
                    int idx = m_ObjectList.IndexOf(current[i]);
                    if (idx != -1)
                        selobjs.Add(idx);
                }
                m_ListView.Selection = selobjs.ToArray();
                break;
            }
            case Filter.Occludee:
            {
                m_ListView.onDrawItemDelegate = delegate (int idx)
                {
                    if (m_Ocludee[idx] == null)
                    {
                        GUILayout.Label("deleted");
                        return;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    GUILayout.Label(m_Ocludee[idx].name);
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    GUILayout.BeginVertical();
                    GUILayout.Label(m_Ocludee[idx].m_MeshRenderer.Length.ToString(), GUILayout.Width(40));
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                };
                m_ListView.onUpdateSelection = delegate ()
                {
                    int[] sel = m_ListView.Selection;
                    List<GameObject> list = new List<GameObject>();
                    for (int i = 0; i < sel.Length; i++)
                        list.Add(m_Ocludee[sel[i]].gameObject);
                    Selection.objects = list.ToArray();
                };
                RTOccludee[] script = GameObject.FindObjectsOfType<RTOccludee>();
                m_Ocludee.Clear();
                m_Ocludee.AddRange(script);
                m_ListView.Clear(this);
                m_ListView.SeCount(m_Ocludee.Count);
                m_ListView.Selection = new int[0];

                GameObject[] current = Selection.gameObjects;
                List<int> selobjs = new List<int>();
                for (int i = 0; i < current.Length; i++)
                {
                    RTOccludee o = current[i].GetComponent<RTOccludee>();
                    if (o != null)
                    {
                        int idx = m_Ocludee.IndexOf(o);
                        if (idx != -1)
                            selobjs.Add(idx);
                    }
                }
                m_ListView.Selection = selobjs.ToArray();
                break;
            }
            case Filter.MeshRenderers:
            {
                m_ListView.onDrawItemDelegate = delegate (int idx)
                {
                    if (m_MeshRenderers[idx] == null)
                    {
                        GUILayout.Label("deleted");
                        return;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    GUILayout.Label(m_MeshRenderers[idx].name);

                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    GUILayout.BeginVertical();
                    RTOccludee value;
                    if (m_ObjectOccludee.TryGetValue(m_MeshRenderers[idx], out value))
                        GUILayout.Label(value != null? value.name : "deleted");
                    else
                        GUILayout.Label("");
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                };
                m_ListView.onUpdateSelection = delegate ()
                {
                    int[] sel = m_ListView.Selection;
                    List<GameObject> list = new List<GameObject>();
                    for (int i = 0; i < sel.Length; i++)
                        list.Add(m_MeshRenderers[sel[i]].gameObject);
                    Selection.objects = list.ToArray();
                };

                UpdateMeshrendererOcludeeTable();

                MeshRenderer[] meshrenderer = GameObject.FindObjectsOfType<MeshRenderer>();
                m_MeshRenderers.Clear();
                m_MeshRenderers.AddRange(meshrenderer);

                m_Ocludee.Clear();

                m_ListView.Clear(this);
                m_ListView.SeCount(m_MeshRenderers.Count);
                break;
            }
            case Filter.SelectedMeshRenderer:
            {
                m_ListView.onDrawItemDelegate = delegate (int idx)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    GUILayout.Label(m_MeshRenderers[idx].name);

                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    GUILayout.BeginVertical();
                    RTOccludee value;
                    if (m_ObjectOccludee.TryGetValue(m_MeshRenderers[idx], out value))
                    {
                        GUILayout.Label(value != null ? value.name : "destroyed");
                    }
                    else
                        GUILayout.Label("");
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                };
                m_ListView.onUpdateSelection = delegate ()
                {
                    int[] sel = m_ListView.Selection;
                    List<GameObject> list = new List<GameObject>();
                    for (int i = 0; i < sel.Length; i++)
                        list.Add(m_MeshRenderers[sel[i]].gameObject);
                    Selection.objects = list.ToArray();
                };

                m_MeshRenderers.Clear();
                GameObject[] objs = Selection.gameObjects;
                for (int i = 0; i < objs.Length; i++)
                {
                    GameObject o = (GameObject)objs[i];
                    bool branch = false;

                    for (Transform t = o.transform.parent; t != null; t = t.parent)
                    {
                        for (int j = 0; j < objs.Length; j++)
                        {
                            if (t.gameObject == objs[j])
                            {
                                branch = true;
                                break;
                            }
                        }
                    }

                    if (branch == true)
                        continue;

                    MeshRenderer[] m = o.GetComponentsInChildren<MeshRenderer>();
                    if (m.Length > 0)
                        m_MeshRenderers.AddRange(m);
                }

                UpdateMeshrendererOcludeeTable();

                m_ListView.Clear(this);
                m_ListView.SeCount(m_MeshRenderers.Count);

                List<int> selidx = new List<int>();
                List<Object> selobjs = new List<Object>();
                for (int i = 0; i < m_MeshRenderers.Count; i++)
                {
                    selidx.Add(i);
                    selobjs.Add(m_MeshRenderers[i].gameObject);
                }
                m_ListView.Selection = selidx.ToArray();
                Selection.objects = selobjs.ToArray();
                break;
            }
        }
        if (filter == Filter.SelectedMeshRenderer || filter == Filter.MeshRenderers)
        {
            GameObject[] current = Selection.gameObjects;
            List<int> selobjs = new List<int>();
            for (int i = 0; i < current.Length; i++)
            {
                MeshRenderer o = current[i].GetComponent<MeshRenderer>();
                if (o != null)
                {
                    int idx = m_MeshRenderers.IndexOf(o);
                    if (idx != -1)
                        selobjs.Add(idx);
                }
            }
            m_ListView.Selection = selobjs.ToArray();
        }
    }

    void UpdateMeshrendererOcludeeTable()
    {
        RTOccludee[] ocludee = GameObject.FindObjectsOfType<RTOccludee>();
        m_ObjectOccludee.Clear();

        for (int j = 0; j < ocludee.Length; j++)
        {
            for (int i = 0; i < ocludee[j].m_MeshRenderer.Length; i++)
            {
                if (!m_ObjectOccludee.ContainsKey(ocludee[j].m_MeshRenderer[i]))
                    m_ObjectOccludee.Add(ocludee[j].m_MeshRenderer[i], ocludee[j]);
                else
                    Debug.Log(ocludee[j].m_MeshRenderer[i].name + ":Overlap with " + m_ObjectOccludee[ocludee[j].m_MeshRenderer[i]].name + " and " + ocludee[j].name);
            }
        }
    }

    void Sort<T>(List<T> list, System.Comparison<T> comparer)
    {
        int[] selection = m_ListView.Selection;

        List<T> sel = new List<T>();
        List<int> newsel = new List<int>();
        for (int i = 0; i < selection.Length; i++)
            sel.Add(list[selection[i]]);
        list.Sort(comparer);
        for (int i = 0; i<list.Count; i++)
        {
            if (sel.IndexOf(list[i]) != -1)
                newsel.Add(i);
        }
        m_ListView.Selection = newsel.ToArray();
    }

    void SortByName()
    {
        if (m_Filter == Filter.Occluder)
        {
            Sort<GameObject>(m_ObjectList, (GameObject o1, GameObject o2) => { return o1.name.CompareTo(o2.name); });
            Repaint();

        }
        else if (m_Filter == Filter.Occludee)
        {
            Sort<RTOccludee>(m_Ocludee, (RTOccludee o1, RTOccludee o2) => { return o1.name.CompareTo(o2.name); });
            Repaint();
        }
        else
        {
            Sort<MeshRenderer>(m_MeshRenderers, (MeshRenderer o1, MeshRenderer o2) => { return o1.name.CompareTo(o2.name); });
            Repaint();
        }
    }

    void SortByOccludee()
    {
        Sort<MeshRenderer>(m_MeshRenderers, (MeshRenderer m1, MeshRenderer m2) => {
            RTOccludee o1 = null, o2 = null;
            m_ObjectOccludee.TryGetValue(m1, out o1);
            m_ObjectOccludee.TryGetValue(m2, out o2);
            return o1 == o2 ? 0 : (o1 == null ? -1 : (o2 == null ? 1 : o1.name.CompareTo(o2.name)));
        });
        Repaint();
    }

    void FilterKeyword(string[] keywords)
    {
        if (m_Filter == Filter.Occluder)
        {
            for (int i = 0; i < m_ObjectList.Count; i++)
            {
                for (int j = 0; j < keywords.Length; j++)
                {
                    if (m_ObjectList[i].name.IndexOf(keywords[j], System.StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        m_KeywordSelection.Add(i);
                        break;
                    }
                }
            }
        }
        else if (m_Filter == Filter.Occludee)
        {
            for (int i = 0; i < m_Ocludee.Count; i++)
            {
                for (int j = 0; j < keywords.Length; j++)
                {
                    if (m_Ocludee[i].name.IndexOf(keywords[j], System.StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        m_KeywordSelection.Add(i);
                        break;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < m_MeshRenderers.Count; i++)
            {
                for (int j = 0; j < keywords.Length; j++)
                {
                    if (m_MeshRenderers[i].name.IndexOf(keywords[j], System.StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        m_KeywordSelection.Add(i);
                        break;
                    }
                }
            }
        }
    }

    string m_Keyword = "";
    bool m_KeywordDiry = false;
    List <int> m_KeywordSelection = new List<int>();

    void OnGUI()
    {
        Event guiEvent = Event.current;

        DrawToolBar();

        GUILayout.BeginHorizontal(GUI.skin.box);
        m_ListView.Draw(guiEvent, m_Focus);
        GUILayout.EndHorizontal();
    }

    void DrawToolBar()
    {
        Event guiEvent = Event.current;
        float scale = position.width > 900 ? 1.0f : position.width / 900.0f;

        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        m_SelectFilter = (Filter)EditorGUILayout.EnumPopup("", m_SelectFilter, EditorStyles.toolbarPopup, GUILayout.Width(200 * scale));
        if (GUILayout.Button("Collect", EditorStyles.toolbarButton, GUILayout.Width(100 * scale)))
        {
            m_Filter = m_SelectFilter;
            m_KeywordDiry = true;
            UpdateItems(m_Filter);
        }
        GUILayout.FlexibleSpace();

        string keyword = GUILayout.TextField(m_Keyword, ToolAsset.FindCustomStyle("ToolbarSeachTextField"), GUILayout.Width(120 * scale));
        if (m_Keyword != keyword || m_KeywordDiry == true)
        {
            m_Keyword = keyword;
            m_KeywordDiry = false;
            m_KeywordSelection.Clear();

            List<string> keywords = new List<string>(keyword.Split(','));
            while (keywords.Remove("") == true) ;

            FilterKeyword(keywords.ToArray());
        }
        if (GUILayout.Button(new GUIContent(""), ToolAsset.FindCustomStyle("ToolbarSeachCancelButton")))
        {
            m_Keyword = "";
            m_KeywordDiry = false;
            m_KeywordSelection.Clear();
            FilterKeyword(new string[0]);
        }
        EditorGUI.BeginDisabledGroup(m_KeywordSelection.Count == 0 ? true : false);
        if (GUILayout.Button(m_KeywordSelection.Count == 0 ? "Select" : "Select(" + m_KeywordSelection.Count + ")", EditorStyles.toolbarButton, GUILayout.Width(100 * scale)))
        {
            m_ListView.Selection = m_KeywordSelection.ToArray();

            int[] selection = m_ListView.Selection;
            List<GameObject> selobj = new List<GameObject>();

            if (m_Filter == Filter.Occluder)
            {
                for (int i = 0; i < selection.Length; i++)
                    selobj.Add(m_ObjectList[selection[i]]);
            }
            else if (m_Filter == Filter.Occludee)
            {
                for (int i = 0; i < selection.Length; i++)
                    selobj.Add(m_Ocludee[selection[i]].gameObject);
            }
            else
            {
                for (int i = 0; i < selection.Length; i++)
                    selobj.Add(m_MeshRenderers[selection[i]].gameObject);
            }
            Selection.objects = selobj.ToArray();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Sort", EditorStyles.toolbarDropDown, GUILayout.Width(100 * scale)))
        {
            GenericMenu toolsmenu = new GenericMenu();
            toolsmenu.AddItem(new GUIContent("by Name"), false, () => { SortByName(); });

            if (m_Filter != Filter.Occludee && m_Filter != Filter.Occluder)
                toolsmenu.AddItem(new GUIContent("by Occludee"), false, () => { SortByOccludee(); });
            toolsmenu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0f, 0f));
        }

        GUILayout.Space(8);
        if (GUILayout.Button("Remove", EditorStyles.toolbarDropDown, GUILayout.Width(100 * scale)))
        {
            GenericMenu toolsmenu = new GenericMenu();

            if (m_Filter == Filter.Occluder)
            {
                toolsmenu.AddItem(new GUIContent("Selection"), false, () =>
                {
                    List<int> sel = new List<int>();
                    sel.AddRange(m_ListView.Selection);
                    List<GameObject> newobj = new List<GameObject>();
                    for (int i = 0; i < m_ObjectList.Count; i++)
                    {
                        if (sel.IndexOf(i) == -1)
                            newobj.Add(m_ObjectList[i]);
                    }
                    m_ObjectList = newobj;
                    m_ListView.SeCount(m_ObjectList.Count);
                    m_ListView.Selection = new int[0];
                    Selection.objects = new Object[0];
                    m_KeywordDiry = true;
                });
                toolsmenu.AddItem(new GUIContent("Unselection"), false, () =>
                {
                    List<int> sel = new List<int>();
                    sel.AddRange(m_ListView.Selection);
                    List<GameObject> newobj = new List<GameObject>();
                    for (int i = 0; i < m_ObjectList.Count; i++)
                    {
                        if (sel.IndexOf(i) != -1)
                            newobj.Add(m_ObjectList[i]);
                    }
                    m_ObjectList = newobj;
                    List<int> newsel = new List<int>();
                    for (int i = 0; i < m_ObjectList.Count; i++)
                        newsel.Add(i);
                    m_ListView.SeCount(m_ObjectList.Count);
                    m_ListView.Selection = newsel.ToArray();
                    m_KeywordDiry = true;
                });
            }
            else if (m_Filter == Filter.Occludee)
            {
                toolsmenu.AddItem(new GUIContent("Selection"), false, () =>
                {
                    List<int> sel = new List<int>();
                    sel.AddRange(m_ListView.Selection);
                    List<RTOccludee> newobj = new List<RTOccludee>();
                    for (int i = 0; i < m_Ocludee.Count; i++)
                    {
                        if (sel.IndexOf(i) == -1)
                            newobj.Add(m_Ocludee[i]);
                    }
                    m_Ocludee = newobj;
                    m_ListView.SeCount(m_Ocludee.Count);
                    m_ListView.Selection = new int[0];
                    Selection.objects = new Object[0];
                    m_KeywordDiry = true;
                });
                toolsmenu.AddItem(new GUIContent("Unselection"), false, () =>
                {
                    List<int> sel = new List<int>();
                    sel.AddRange(m_ListView.Selection);
                    List<RTOccludee> newobj = new List<RTOccludee>();
                    for (int i = 0; i < m_Ocludee.Count; i++)
                    {
                        if (sel.IndexOf(i) != -1)
                            newobj.Add(m_Ocludee[i]);
                    }
                    m_Ocludee = newobj;
                    List<int> newsel = new List<int>();
                    for (int i = 0; i < m_Ocludee.Count; i++)
                        newsel.Add(i);
                    m_ListView.SeCount(m_Ocludee.Count);
                    m_ListView.Selection = newsel.ToArray();
                    m_KeywordDiry = true;
                });
            }
            else if (m_Filter != Filter.Occludee && m_Filter != Filter.Occluder)
            {
                toolsmenu.AddItem(new GUIContent("Occludee"), false, () =>
                {
                    List<int> selection = new List<int>(m_ListView.Selection);
                    List<int> newsel = new List<int>();
                    List<MeshRenderer> newobj = new List<MeshRenderer>();
                    List<GameObject> selobj = new List<GameObject>();

                    for (int i = 0; i < m_MeshRenderers.Count; i++)
                    {
                        if (!m_ObjectOccludee.ContainsKey(m_MeshRenderers[i]))
                        {
                            newobj.Add(m_MeshRenderers[i]);
                            if (selection.IndexOf(i) != -1)
                            {
                                newsel.Add(newobj.Count - 1);
                                selobj.Add(m_MeshRenderers[i].gameObject);
                            }
                        }
                    }
                    m_MeshRenderers = newobj;
                    m_ListView.SeCount(m_MeshRenderers.Count);
                    m_ListView.Selection = newsel.ToArray();
                    Selection.objects = selobj.ToArray();
                    m_KeywordDiry = true;
                });
                toolsmenu.AddItem(new GUIContent("Disabled Meshrenderers"), false, () =>
                {
                    List<int> selection = new List<int>(m_ListView.Selection);
                    List<int> newsel = new List<int>();
                    List<MeshRenderer> newobj = new List<MeshRenderer>();
                    List<GameObject> selobj = new List<GameObject>();

                    for (int i = 0; i < m_MeshRenderers.Count; i++)
                    {
                        if (m_MeshRenderers[i].enabled && m_MeshRenderers[i].gameObject.activeInHierarchy)
                        {
                            newobj.Add(m_MeshRenderers[i]);
                            if (selection.IndexOf(i) != -1)
                            {
                                newsel.Add(newobj.Count - 1);
                                selobj.Add(m_MeshRenderers[i].gameObject);
                            }
                        }
                    }
                    m_MeshRenderers = newobj;
                    m_ListView.SeCount(m_MeshRenderers.Count);
                    m_ListView.Selection = newsel.ToArray();
                    Selection.objects = selobj.ToArray();
                    m_KeywordDiry = true;
                });
                toolsmenu.AddSeparator("");

                toolsmenu.AddItem(new GUIContent("Selection"), false, () =>
                {
                    List<int> sel = new List<int>();
                    sel.AddRange(m_ListView.Selection);
                    List<MeshRenderer> newobj = new List<MeshRenderer>();
                    for (int i = 0; i < m_MeshRenderers.Count; i++)
                    {
                        if (sel.IndexOf(i) == -1)
                            newobj.Add(m_MeshRenderers[i]);
                    }
                    m_MeshRenderers = newobj;
                    m_ListView.SeCount(m_MeshRenderers.Count);
                    m_ListView.Selection = new int[0];
                    Selection.objects = new Object[0];
                    m_KeywordDiry = true;
                });
                toolsmenu.AddItem(new GUIContent("Unselection"), false, () =>
                {
                    List<int> sel = new List<int>();
                    sel.AddRange(m_ListView.Selection);
                    List<MeshRenderer> newobj = new List<MeshRenderer>();
                    for (int i = 0; i < m_MeshRenderers.Count; i++)
                    {
                        if (sel.IndexOf(i) != -1)
                            newobj.Add(m_MeshRenderers[i]);
                    }
                    m_MeshRenderers = newobj;
                    List<int> newsel = new List<int>();
                    for (int i = 0; i < m_MeshRenderers.Count; i++)
                        newsel.Add(i);
                    m_ListView.SeCount(m_MeshRenderers.Count);
                    m_ListView.Selection = newsel.ToArray();
                    m_KeywordDiry = true;
                });
            }

            toolsmenu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0f, 0f));
        }

        GUILayout.Space(8);

        GameObject[] selobjs = Selection.gameObjects;
        List<MeshRenderer> selrenderer = new List<MeshRenderer>();

        for (int i = 0; i < selobjs.Length; i++)
        {
            MeshRenderer renderer = selobjs[i].GetComponent<MeshRenderer>();
            if (renderer != null)
                selrenderer.Add(renderer);
        }

        if (GUILayout.Button("Create From Selection", EditorStyles.toolbarDropDown, GUILayout.Width(150 * scale)))
        {
            GenericMenu toolsmenu = new GenericMenu();

            if (selrenderer.Count == 0)
            {
                toolsmenu.AddDisabledItem(new GUIContent("No renderer selection"));
            }
            else
            {
                toolsmenu.AddItem(new GUIContent("Occluder"), false, () => {
                    Selection.objects = new Object[] { CreateOccluerFromSelection(selrenderer.ToArray(), 0, 1) };
                    EditorUtility.ClearProgressBar();
                });
                toolsmenu.AddItem(new GUIContent("Occludee"), false, () => {
                    Selection.objects = new Object[] { CreateOcclueeFromSelection(selrenderer.ToArray(), 0, 1) };
                    EditorUtility.ClearProgressBar();
                });

                if (selrenderer.Count > 1)
                {
                    toolsmenu.AddSeparator("");

                    toolsmenu.AddItem(new GUIContent("Occluder(Foreach)"), false, () => {
                        List<Object> selection = new List<Object>();
                        for (int i=0; i< selrenderer.Count; i++)
                            CreateOccluerFromSelection(new MeshRenderer[] { selrenderer[i] }, (float)i/ selrenderer.Count, (float)(i+1) / selrenderer.Count);
                        Selection.objects = selection.ToArray();
                        EditorUtility.ClearProgressBar();
                    });
                    toolsmenu.AddItem(new GUIContent("Occludee(Foreach)"), false, () => {
                        List<Object> selection = new List<Object>();
                        for (int i = 0; i < selrenderer.Count; i++)
                            selection.Add(CreateOcclueeFromSelection(new MeshRenderer[] { selrenderer[i] }, (float)i / selrenderer.Count, (float)(i + 1) / selrenderer.Count));
                        Selection.objects = selection.ToArray();
                        EditorUtility.ClearProgressBar();
                    });
                }
                toolsmenu.AddSeparator("");
                toolsmenu.AddItem(new GUIContent("Attach Occludee Component (Foreach)"), false, () => {
                    AttachOcclueeComponent(selrenderer.ToArray(), 0, 1);
                    EditorUtility.ClearProgressBar();
                });
            }
            toolsmenu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0f, 0f));
        }
        GUILayout.EndHorizontal();

        if (guiEvent.type == EventType.MouseDown)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            if (!rect.Contains(Event.current.mousePosition))
                GUI.FocusControl(null);
        }
    }

    static Vector2[] Simpler(Vector2[] vertices)
    {
        List<Vector2> v = new List<Vector2>(vertices);

        while (v.Count > 4)
        {
            float max = Mathf.Cos(Mathf.Deg2Rad * 140.0f);
            int match = -1;
            for (int i = 0; i < v.Count; i++)
            {
                Vector2 v1 = v[(i - 1 + v.Count) % v.Count] - v[i];
                Vector2 v2 = v[(i + 1 + v.Count) % v.Count] - v[i];
                float dot = Vector2.Dot(v1.normalized, v2.normalized);
                if (dot < max)
                {
                    max = dot;
                    match = i;
                }
            }
            if (match == -1)
                break;
            v.RemoveAt(match);
        }
        return v.ToArray();
    }    

    Object CreateOccluerFromSelection(MeshRenderer[] selrenderer, float progressmin, float progressmax)
    {
        EditorUtility.DisplayProgressBar("CreateOccluerFromSelection", "", Mathf.Lerp(progressmin, progressmax, 0.0f));

        List<Vector3> vertices = new List<Vector3>();
        for (int i = 0; i < selrenderer.Length; i++)
        {
            MeshFilter f = selrenderer[i].GetComponent<MeshFilter>();
            if (f != null)
            {
                for (int j = 0; j < f.sharedMesh.vertexCount; j++)
                    vertices.Add(selrenderer[i].transform.TransformPoint(f.sharedMesh.vertices[j]));
            }
        }

        Bounds bounds = new Bounds(vertices[0], Vector3.zero);
        for (int i = 1; i < vertices.Count; i++)
            bounds.Encapsulate(vertices[i]);

        EditorUtility.DisplayProgressBar("CreateOccluerFromSelection", "", Mathf.Lerp(progressmin, progressmax, 0.2f));

        GameObject o = new GameObject();
        o.transform.localPosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        o.transform.localRotation = Quaternion.identity;// selrenderer.Length == 1 ? selrenderer[0].transform.rotation : Quaternion.identity;
        o.transform.localScale = Vector3.one;
        o.name = "Occluder_" + selrenderer[0].gameObject.name;

        for (int i = 0; i < vertices.Count; i++)
            vertices[i] = o.transform.InverseTransformPoint(vertices[i]);

        List<Vector2> proj = new List<Vector2>(vertices.Count);
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector2 v = new Vector2(vertices[i].x, vertices[i].z);
            int j;
            for (j = 0; j < proj.Count; j++)
            {
                if (proj[j] == v)
                    break;
            }
            if (j == proj.Count)
                proj.Add(v);
        }

        EditorUtility.DisplayProgressBar("CreateOccluerFromSelection", "", Mathf.Lerp(progressmin, progressmax, 0.5f));

        Vector2[] convex = RT.Occluder.ConvexHull2d(proj.ToArray());
        Vector2[] simpler = Simpler(convex);

        EditorUtility.DisplayProgressBar("CreateOccluerFromSelection", "", Mathf.Lerp(progressmin, progressmax, 1.0f));

        RTConvexTowerOccluder occluder = o.AddComponent<RTConvexTowerOccluder>();
        Vector3 size = bounds.size;
        occluder.m_Points = new List <Vector2> (simpler);
        occluder.m_Height = size.y;
        Undo.RegisterCreatedObjectUndo(o, "Create Occluder");

        return (Object)o;
    }

    Object CreateOcclueeFromSelection(MeshRenderer[] selection, float progressmin, float progressmax)
    {
        EditorUtility.DisplayProgressBar("CreateOcclueeFromSelection", "", Mathf.Lerp(progressmin, progressmax, 0.0f));

        List<Material> sortkey = new List<Material>();
        List<MeshRenderer> selrenderer = new List<MeshRenderer>(selection);
        Vector3 position = Vector3.zero;

        if (selrenderer.Count == 1)
            position = selrenderer[0].transform.position;

        for (int i = 0; i < selrenderer.Count; i++)
            sortkey.Add(selrenderer[i].sharedMaterial);

        selrenderer.Sort(delegate (MeshRenderer m1, MeshRenderer m2)
        {
            int d = sortkey.IndexOf(m1.sharedMaterial) - sortkey.IndexOf(m2.sharedMaterial);
            return d == 0 ? (int)(m1.transform.position.y - m2.transform.position.y) : d;
        });

        EditorUtility.DisplayProgressBar("CreateOcclueeFromSelection", "", Mathf.Lerp(progressmin, progressmax, 0.2f));

        GameObject o = new GameObject();
        o.transform.localPosition = position;
        o.transform.localRotation = Quaternion.identity;
        o.transform.localScale = Vector3.one;
        o.name = "Occludee_" + selrenderer[0].gameObject.name + (selrenderer.Count > 1 ? "(+" + (selrenderer.Count - 1) + ")" : "");
        RTOccludee occludee = o.AddComponent<RTOccludee>();
        occludee.m_MeshRenderer = selrenderer.ToArray();

        EditorUtility.DisplayProgressBar("CreateOcclueeFromSelection", "", Mathf.Lerp(progressmin, progressmax, 0.5f));

        for (int i = 0; i < selrenderer.Count; i++)
        {
            if (!m_ObjectOccludee.ContainsKey(selrenderer[i]))
                m_ObjectOccludee.Add(selrenderer[i], occludee);
            else
            {
                Debug.Log(selrenderer[i].name + ":Overlap with " + m_ObjectOccludee[selrenderer[i]].name + " and " + occludee.name);
            }
        }

        Undo.RegisterCreatedObjectUndo(o, "Create Occludee");

        EditorUtility.DisplayProgressBar("CreateOcclueeFromSelection", "", Mathf.Lerp(progressmin, progressmax, 1.0f));
        return (Object)o;
    }

    void AttachOcclueeComponent(MeshRenderer[] selection, float progressmin, float progressmax)
    {
        for (int i = 0; i < selection.Length; i++)
        {
            EditorUtility.DisplayProgressBar("AttachOcclueeComponent", "", Mathf.Lerp(progressmin, progressmax, (float)i / selection.Length));

            GameObject o = selection[i].gameObject;
            Undo.RecordObject(selection[i], "Attach Occluee");
            RTOccludee occludee = o.AddComponent<RTOccludee>();
            occludee.m_MeshRenderer = new MeshRenderer[] { selection[i] };
            m_ObjectOccludee.Add(selection[i], occludee);
        }

        EditorUtility.DisplayProgressBar("AttachOcclueeComponent", "", Mathf.Lerp(progressmin, progressmax, 1.0f));
    }

    #region List View
    class ListView
    {
        public delegate void OnDrawItem(int index);
        public OnDrawItem onDrawItemDelegate = null;

        public delegate void OnUpdateSelection();
        public OnUpdateSelection onUpdateSelection = null;

        public int[] Selection
        {
            get { return m_SelectItem.ToArray(); }
            set { m_SelectItem.Clear(); m_SelectItem.AddRange(value); m_Window.Repaint(); }
        }

        public int SelectionCount { get { return m_SelectItem.Count; } }

        public void Clear(EditorWindow window)
        {
            m_Scroll = Vector2.zero;
            m_Window = window;
        }

        public void SeCount(int count)
        {
            m_Count = count;
        }

        void DrawList(Event guiEvent, bool focus)
        {
            int margin = 0;
            int y2 = Mathf.Min(m_Count, (int)((m_LayoutOffset + m_ListViewRect.height) / 22) + 1 + margin);
            int y1 = Mathf.Max(0, (int)(m_LayoutOffset / 22) - margin);

            if (y1 > 0)
                FillSpace(y1 * 22);

            if (guiEvent.type == EventType.Repaint)
            {
                m_ListItemRect.Clear();
                m_ListItemOffset = y1;
            }

            for (int i = y1; i < y2; i++)
            {
                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginHorizontal(m_SelectItem.IndexOf(i) != -1 ? (focus ? ToolAsset.select : ToolAsset.select_defocus) : ToolAsset.blank, GUILayout.Height(22));
                onDrawItemDelegate(i);
                EditorGUILayout.EndHorizontal();

                if (guiEvent.type == EventType.Repaint)
                    m_ListItemRect.Add(GUILayoutUtility.GetLastRect());
            }

            if (y2 < m_Count)
                FillSpace((m_Count - y2) * 22);
        }

        void FillSpace(float h)
        {
            EditorGUILayout.BeginHorizontal(ToolAsset.blank, GUILayout.Height(h));
            GUILayout.Label("");
            EditorGUILayout.EndHorizontal();
        }

        List<Rect> m_ListItemRect = new List<Rect>();
        int m_ListItemOffset = 0;

        List<int> m_SelectItem = new List<int>();
        Vector2 m_Scroll = Vector2.zero;
        int m_Count;
        EditorWindow m_Window;

        float m_LayoutOffset;

        public void Draw(Event guiEvent, bool focus = true)
        {
            m_Scroll = GUILayout.BeginScrollView(m_Scroll);

            if (guiEvent.type == EventType.Layout)
                m_LayoutOffset = m_Scroll.y;

            if (guiEvent.type == EventType.Repaint || guiEvent.type == EventType.Layout)
            {
                Color prevColor = GUI.backgroundColor;
                DrawList(guiEvent, focus);
                GUI.backgroundColor = prevColor;
            }
            GUILayout.EndScrollView();
            Rect rect = GUILayoutUtility.GetLastRect();

            if (guiEvent.type == EventType.Repaint)
                m_ListViewRect = rect;

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && rect.Contains(guiEvent.mousePosition))
            {
                Click(guiEvent, guiEvent.mousePosition - rect.position + m_Scroll);
            }
            if (guiEvent.type == EventType.ValidateCommand && guiEvent.commandName == "SelectAll")
            {
                SelectSingleItem(0, true);
                SelectContinousItem(m_Count - 1, true);
                m_Window.Repaint();
            }
            if (guiEvent.type == EventType.KeyDown &&
                (guiEvent.keyCode == KeyCode.UpArrow || guiEvent.keyCode == KeyCode.DownArrow || guiEvent.keyCode == KeyCode.End || guiEvent.keyCode == KeyCode.Home))
            {
                InputKeySelect(guiEvent.keyCode, guiEvent.control == true || guiEvent.shift == true ? true : false);
            }
        }

        bool Click(Event guiEvent, Vector2 pos)
        {
            for (int i = 0; i < m_ListItemRect.Count; i++)
            {
                if (m_ListItemRect[i].Contains(pos))
                {
                    int item = m_ListItemOffset + i;

                    if (guiEvent.control == true)
                        ToggleSelectItem(item);
                    else if (guiEvent.shift == true)
                        SelectContinousItem(item);
                    else
                        SelectSingleItem(item);

                    m_Window.Repaint();
                    return true;
                }
            }
            return false;
        }

        void InputKeySelect(KeyCode keycode, bool shift)
        {
            if (keycode == KeyCode.Home)
            {
                if (shift)
                    SelectContinousItem(0, true);
                else
                    SelectSingleItem(0, true);
                m_Window.Repaint();
            }
            else if (keycode == KeyCode.End)
            {
                if (shift)
                    SelectContinousItem(m_Count - 1, true);
                else
                    SelectSingleItem(m_Count - 1, true);
                m_Window.Repaint();
            }
            else if (keycode == KeyCode.UpArrow)
            {
                if (shift)
                    SelectContinousItem(Mathf.Clamp(m_LastSelect - 1, 0, m_Count - 1), true);
                else
                    SelectSingleItem(Mathf.Clamp(m_LastSelect - 1, 0, m_Count - 1), true);
                m_Window.Repaint();
            }
            else if (keycode == KeyCode.DownArrow)
            {
                if (shift)
                    SelectContinousItem(Mathf.Clamp(m_LastSelect + 1, 0, m_Count - 1), true);
                else
                    SelectSingleItem(Mathf.Clamp(m_LastSelect + 1, 0, m_Count - 1), true);
                m_Window.Repaint();
            }
        }

        Rect m_ListViewRect;
        int m_ShiftAnchor = 0;
        int m_LastSelect;

        void ToggleSelectItem(int item)
        {
            if (m_SelectItem.IndexOf(item) != -1)
                m_SelectItem.Remove(item);
            else
                m_SelectItem.Add(item);
            onUpdateSelection();
            m_LastSelect = item;
        }

        void SelectContinousItem(int item, bool updateposition = false)
        {
            if (m_SelectItem.Count == 0)
            {
                m_SelectItem.Add(item);
                m_ShiftAnchor = 0;
            }
            else
            {
                int shiftmin = -1, shiftmax = -1;
                for (int i = 0; i < m_SelectItem.Count; i++)
                {
                    if (shiftmin == -1 || m_SelectItem[i] < shiftmin)
                        shiftmin = m_SelectItem[i];
                    if (shiftmax == -1 || m_SelectItem[i] > shiftmax)
                        shiftmax = m_SelectItem[i];
                }

                if (item < shiftmin)
                    m_ShiftAnchor = 1;
                else if (item > shiftmax)
                    m_ShiftAnchor = 0;

                m_SelectItem.Clear();
                if (m_ShiftAnchor == 1)
                {
                    for (int i = item; i <= shiftmax; i++)
                        m_SelectItem.Add(i);
                }
                else
                {
                    for (int i = shiftmin; i <= item; i++)
                        m_SelectItem.Add(i);
                }
            }

            if (updateposition == true)
                UpdateSelectPosition(item);

            onUpdateSelection();
            m_LastSelect = item;
        }

        void SelectSingleItem(int item, bool updateposition = false)
        {
            m_SelectItem.Clear();
            m_SelectItem.Add(item);

            if (updateposition == true)
                UpdateSelectPosition(item);

            onUpdateSelection();
            m_LastSelect = item;
        }

        void UpdateSelectPosition(int selectidx)
        {
            if (selectidx * 22 < m_Scroll.y)
                m_Scroll.y = selectidx * 22;

            if (m_Count * 22 > m_ListViewRect.height && (selectidx + 1) * 22 > m_Scroll.y + m_ListViewRect.height)
                m_Scroll.y = (selectidx + 1) * 22 - m_ListViewRect.height;
        }
    };
    #endregion
}