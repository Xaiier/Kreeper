using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using KSP.IO;

namespace Kreeper
{
    public class AssemblyData
    {
        public string name;
        public string version;
        public List<TypeData> types;

        public AssemblyData(string n, string v, List<TypeData> t)
        {
            name = n;
            version = v;
            types = t;
        }
    }

    public class TypeData
    {
        public Type type;
        public FieldInfo[] fields;
        public MethodInfo[] methods;
        public string name;

        public TypeData(Type t)
        {
            type = t;
            fields = t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            name = t.Name;
        }
    }

    public class WatchItem
    {
        public FieldInfo field;
        public UnityEngine.Object obj;

        public WatchItem(FieldInfo f)
        {
            field = f;
            obj = UnityEngine.Object.FindObjectOfType(f.DeclaringType);
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class Kreeper : MonoBehaviour
    {
        Rect window = new Rect(0, 0, 800, 0);
        List<AssemblyData> assemblies = new List<AssemblyData>();

        //EXPLORE
        string assemblySearch = "";
        string typeSearch = "";
        string variableSearch = "";
        string methodSearch = "";
        Vector2 assemblyScroll = new Vector2(0, 0);
        int selectedAssembly = 0;
        Vector2 typeScroll = new Vector2(0, 0);
        Vector2 variableScroll = new Vector2(0, 0);
        Vector2 methodScroll = new Vector2(0, 0);
        TypeData currentType;

        bool showSelector = true;

        //WATCH
        List<WatchItem> watchList = new List<WatchItem>();
        Vector2 watchScroll = new Vector2(0, 0);

        Color highlightColor = XKCDColors.LightBlue;
        Color publicColor = new Color(200/255, 255/255, 200/255, 1);
        Color privateColor = new Color(255 / 255, 200 / 255, 200 / 255, 1);
        Color staticColor = new Color(255 / 255, 255 / 255, 200 / 255, 1);

        bool[] activeModes = new bool[5];
        string[] modeNames = { "Explore", "Watch", "Execute", "Logs", "Memory" };

        private void Start()
        {
            UnityEngine.Object.DontDestroyOnLoad(this);

            using (IEnumerator<AssemblyLoader.LoadedAssembly> enumerator = AssemblyLoader.loadedAssemblies.GetEnumerator())//this is what the compiler makes a foreach into
            {
                while (enumerator.MoveNext())
                {
                    AssemblyLoader.LoadedAssembly current = enumerator.Current;

                    string name = current.assembly.FullName.Substring(0, current.assembly.FullName.IndexOf(','));
                    string version = current.assembly.FullName.Substring(current.assembly.FullName.IndexOf(','), current.assembly.FullName.IndexOf(',',current.assembly.FullName.IndexOf(',')));//TODO this doesn't work

                    List<TypeData> types = new List<TypeData>();
                    foreach (Type t in current.assembly.GetTypes())
                    {
                        types.Add(new TypeData(t));
                    }

                    assemblies.Add(new AssemblyData(name, version, types));
                }
            }

            currentType = assemblies[0].types[0];
        }
        private void Awake()
        {

        }
        private void Update()
        {
            window.width = 800;
            window.height = 0;
        }
        private void OnGUI()
        {
            window = GUILayout.Window(1, window, onWindow, "Kreeper");
        }
        private void onWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            {
                for (int i = 0; i < activeModes.Length; i++)
                {
                    activeModes[i] = GUILayout.Toggle(activeModes[i], modeNames[i], "button");
                }
            }
            GUILayout.EndHorizontal();

            int height = 800;
            if (anyTrue(activeModes))
            {
                height = 800 / numTrue(activeModes);
            }

            if (activeModes[0])
            {
                onExplore(height);
            }
            if (activeModes[1])
            {
                onWatch(height);
            }
            if (activeModes[2])
            {
                onExecute(height);
            }
            if (activeModes[3])
            {
                onLogs(height);
            }
            if (activeModes[4])
            {
                onMemory(height);
            }

            GUI.DragWindow();
        }
        private void onExplore(int height)
        {
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;

            GUILayout.BeginHorizontal("box", GUILayout.Height(height));
            {
                if (showSelector)
                {
                    GUILayout.BeginVertical(GUILayout.Width(150));
                    {
                        GUILayout.Label("Assemblies");
                        assemblySearch = GUILayout.TextField(assemblySearch);
                        assemblyScroll = GUILayout.BeginScrollView(assemblyScroll, "box");
                        {
                            GUILayout.BeginVertical();
                            {
                                int i = 0;
                                foreach (AssemblyData ad in assemblies)
                                {
                                    if (ad.name.ToLower().StartsWith(assemblySearch.ToLower()) || ad.name.ToLower().Contains(assemblySearch.ToLower()))
                                    {
                                        if (selectedAssembly == i)
                                        {
                                            GUI.contentColor = highlightColor;
                                        }
                                        if (GUILayout.Button(ad.name))
                                        {
                                            selectedAssembly = i;
                                            typeScroll = new Vector2(0, 0);
                                        }
                                        GUI.contentColor = Color.white;
                                    }
                                    i++;
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(GUILayout.Width(200));
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Types");
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("<"))
                            {
                                showSelector = false;
                            }
                        }
                        GUILayout.EndHorizontal();
                        typeSearch = GUILayout.TextField(typeSearch);
                        typeScroll = GUILayout.BeginScrollView(typeScroll, "box");
                        {
                            GUILayout.BeginVertical();
                            {
                                int i = 0;
                                foreach (TypeData td in assemblies[selectedAssembly].types)
                                {
                                    if (td.name.ToLower().StartsWith(typeSearch.ToLower()) || td.name.ToLower().Contains(typeSearch.ToLower()))
                                    {
                                        if (currentType == td)
                                        {
                                            GUI.contentColor = highlightColor;
                                        }
                                        if (GUILayout.Button(td.name))
                                        {
                                            currentType = assemblies[selectedAssembly].types[i];
                                            variableScroll = new Vector2(0, 0);
                                            methodScroll = new Vector2(0, 0);
                                        }
                                        GUI.contentColor = Color.white;
                                    }
                                    i++;
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();
                }
                else
                {
                    GUILayout.BeginVertical();
                    {
                        if (GUILayout.Button(">", GUILayout.Width(20)))
                        {
                            showSelector = true;
                        }
                    }
                    GUILayout.EndVertical();
                }

                onTypeViewer();
            }
            GUILayout.EndHorizontal();

            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        }
        private void onWatch(int height)
        {
            GUILayout.BeginHorizontal("box", GUILayout.Height(height));
            {
                watchScroll = GUILayout.BeginScrollView(watchScroll);
                {
                    foreach (WatchItem w in watchList)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical("box", GUILayout.Width(150));
                            {
                                GUILayout.Label(w.field.FieldType.ToString());
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical("box", GUILayout.Width(200));
                            {
                                GUILayout.Label(w.field.Name.ToString());
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical("box", GUILayout.Width(375));
                            {
                                GUILayout.Label(w.field.GetValue(w.obj).ToString());
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical();
                            {
                                GUI.skin.button.alignment = TextAnchor.UpperLeft;
                                if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
                                {
                                    watchList.Remove(w);
                                    break;
                                }
                                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
        }
        private void onExecute(int height)
        {
            GUILayout.BeginHorizontal("box", GUILayout.Height(height));
            {
                GUILayout.Label("Execute");
            }
            GUILayout.EndHorizontal();
        }
        private void onLogs(int height)
        {
            GUILayout.BeginHorizontal("box", GUILayout.Height(height));
            {
                GUILayout.Label("Logs");
            }
            GUILayout.EndHorizontal();
        }
        private void onMemory(int height)
        {
            GUILayout.BeginHorizontal("box", GUILayout.Height(height));
            {
                GUILayout.Label("Memory");
            }
            GUILayout.EndHorizontal();
        }

        private void onTypeViewer()
        {
            GUILayout.BeginVertical("box", GUILayout.MinWidth(411), GUILayout.MaxWidth(800), GUILayout.ExpandWidth(true));
            {
                GUILayout.Label(currentType.type.Assembly.FullName.Substring(0, currentType.type.Assembly.FullName.IndexOf(',')) + " - " + currentType.name);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical(GUILayout.MinWidth(200), GUILayout.MaxWidth(400), GUILayout.ExpandWidth(true));
                    {
                        GUILayout.Label("Variables");
                        variableSearch = GUILayout.TextField(variableSearch);
                        variableScroll = GUILayout.BeginScrollView(variableScroll, "box");
                        {
                            foreach (FieldInfo f in currentType.fields)
                            {
                                if (f.Name.ToLower().StartsWith(variableSearch.ToLower()) || f.Name.ToLower().Contains(variableSearch.ToLower()))
                                {
                                    if (f.IsPublic)
                                    {
                                        GUI.contentColor = publicColor;
                                    }
                                    if (f.IsPrivate)
                                    {
                                        GUI.contentColor = privateColor;
                                    }
                                    if (f.IsStatic)
                                    {
                                        GUI.contentColor = staticColor;
                                    }
                                    if (GUILayout.Button(f.Name))
                                    {
                                        print(f.GetValue(FindObjectOfType(currentType.type)));
                                        watchList.Add(new WatchItem(f));
                                    }
                                    GUI.contentColor = Color.white;
                                }
                            }
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(GUILayout.MinWidth(200), GUILayout.MaxWidth(400), GUILayout.ExpandWidth(true));
                    {
                        GUILayout.Label("Methods");
                        methodSearch = GUILayout.TextField(methodSearch);
                        methodScroll = GUILayout.BeginScrollView(methodScroll, "box");
                        {
                            foreach (MethodInfo m in currentType.methods)
                            {
                                if (m.Name.ToLower().StartsWith(methodSearch.ToLower()) || m.Name.ToLower().Contains(methodSearch.ToLower()))
                                {
                                    if (GUILayout.Button(m.Name))
                                    {
                                        m.Invoke(FindObjectOfType(currentType.type), null);
                                    }
                                }
                            }
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private bool anyTrue(bool[] array)
        {
            bool any = false;

            foreach (bool b in array)
            {
                any = any || b;
            }

            return any;
        }
        private int numTrue(bool[] array)
        {
            int num = 0;

            foreach (bool b in array)
            {
                if (b)
                {
                    num++;
                }
            }

            return num;
        }
    }
}
   