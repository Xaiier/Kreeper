﻿using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using KSP.IO;

namespace Kreeper
{
    public class AssemblyData
    {
        public string name;
        public string version;
        public List<Type> types;

        public AssemblyData(string n, string v, List<Type> t)
        {
            name = n;
            version = v;
            types = t;
        }
    }

    public class ObjectData
    {
        public object obj;
        public FieldInfo[] fields;
        public MethodInfo[] methods;

        public ObjectData(object o)
        {
            obj = o;
            fields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
            methods = o.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
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

        List<bool> activeModes = new List<bool>() { false, false, false, false, false };
        List<string> modeNames = new List<string>() { "Explore", "Watch", "Execute", "Logs", "Memory" };

        //EXPLORE
        bool showSelector = true;

        string assemblySearch = "";
        string typeSearch = "";
        string primarySearch = "";
        string methodSearch = "";

        Vector2 assemblyScroll = new Vector2(0, 0);
        int selectedAssembly = 0;
        Vector2 typeScroll = new Vector2(0, 0);
        Vector2 primaryScroll = new Vector2(0, 0);
        Vector2 methodScroll = new Vector2(0, 0);

        Color highlightColor = XKCDColors.LightBlue;
        Color publicColor = Color.green;
        Color privateColor = Color.red;
        Color staticColor = Color.yellow;

        Type originType = null;
        List<object> objectHistory = new List<object>();
        ObjectData currentObject = null; //this is just objectHistory's last element, with reflection data

        //WATCH
        List<WatchItem> watchList = new List<WatchItem>();
        Vector2 watchScroll = new Vector2(0, 0);

        private void Start()
        {
            UnityEngine.Object.DontDestroyOnLoad(this);

            foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
            {
                string name = a.name;
                string version = a.assembly.FullName.Substring(a.assembly.FullName.IndexOf('=') + 1, a.assembly.FullName.IndexOf(",", a.assembly.FullName.IndexOf("=")) - (a.assembly.FullName.IndexOf("=") + 1));

                List<Type> types = new List<Type>(a.assembly.GetTypes());

                assemblies.Add(new AssemblyData(name, version, types));
            }
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
                for (int i = 0; i < activeModes.Count; i++)
                {
                    activeModes[i] = GUILayout.Toggle(activeModes[i], modeNames[i], "button");
                }
            }
            GUILayout.EndHorizontal();

            int height = 800;
            if (activeModes.Count(c => c) > 0)
            {
                height = 800 / activeModes.Count(c => c);
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
                        GUILayout.BeginVertical(GUILayout.Width(150)); //ASSEMBLY LIST
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
                                            if (selectedAssembly == i) GUI.contentColor = highlightColor;
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
                        GUILayout.EndVertical(); //END ASSEMBLY LIST

                        GUILayout.BeginVertical(GUILayout.Width(200)); //TYPE LIST
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
                                    foreach (Type t in assemblies[selectedAssembly].types)
                                    {
                                        if (t.Name.ToLower().StartsWith(typeSearch.ToLower()) || t.Name.ToLower().Contains(typeSearch.ToLower()))
                                        {
                                            if (originType == t)
                                            {
                                                GUI.contentColor = highlightColor;
                                            }
                                            if (GUILayout.Button(t.Name))
                                            {
                                                originType = assemblies[selectedAssembly].types[i];
                                                objectHistory.RemoveRange(0, objectHistory.Count);
                                                objectHistory.Add(FindObjectsOfType(t));
                                                currentObject = new ObjectData(objectHistory[objectHistory.Count - 1]);

                                                primaryScroll = new Vector2(0, 0);
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
                        GUILayout.EndVertical();//END TYPE LIST
                    }
                    else //HIDDEN
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
                    //END OF TYPE SELECTOR

                    GUILayout.BeginVertical("box", GUILayout.MinWidth(411), GUILayout.MaxWidth(800), GUILayout.ExpandWidth(true));
                    {
                        if (originType != null)
                        {
                            GUILayout.Label(currentObject.obj.ToString());

                            if (currentObject.obj is IEnumerable)//OBJECT IS A LIST OF OBJECTS
                            {
                                primarySearch = GUILayout.TextField(primarySearch);
                                primaryScroll = GUILayout.BeginScrollView(primaryScroll, "box");
                                {
                                    foreach (var a in (IEnumerable)currentObject.obj)
                                    {
                                        if (a.ToString().ToLower().StartsWith(primarySearch.ToLower()) || a.ToString().ToLower().Contains(primarySearch.ToLower()))
                                        {
                                            if (GUILayout.Button(a.ToString()))
                                            {
                                                objectHistory.Add(a);
                                                currentObject = new ObjectData(a);
                                            }
                                        }
                                    }
                                }
                                GUILayout.EndScrollView();
                            }
                            else //OBJECT IS A CLASS OR VALUE TYPE
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.BeginVertical(GUILayout.MinWidth(200), GUILayout.MaxWidth(400), GUILayout.ExpandWidth(true));
                                    {
                                        GUILayout.Label("Variables");
                                        primarySearch = GUILayout.TextField(primarySearch);
                                        primaryScroll = GUILayout.BeginScrollView(primaryScroll, "box");
                                        {
                                            foreach (FieldInfo f in currentObject.fields)
                                            {
                                                if (f.Name.ToLower().StartsWith(primarySearch.ToLower()) || f.Name.ToLower().Contains(primarySearch.ToLower()))
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
                                                        //TypedReference reference = __makeref(currentObject.obj);
                                                        //objectHistory.Add(f.GetValueDirect(reference));
                                                        //currentObject = new ObjectData(f.GetValueDirect(reference));

                                                        objectHistory.Add(f.GetValue(currentObject.obj));
                                                        currentObject = new ObjectData(f.GetValue(currentObject.obj));
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
                                            foreach (MethodInfo m in currentObject.methods)
                                            {
                                                if (m.Name.ToLower().StartsWith(methodSearch.ToLower()) || m.Name.ToLower().Contains(methodSearch.ToLower()))
                                                {
                                                    if (m.IsPublic)
                                                    {
                                                        GUI.contentColor = publicColor;
                                                    }
                                                    if (m.IsPrivate)
                                                    {
                                                        GUI.contentColor = privateColor;
                                                    }
                                                    if (m.IsStatic)
                                                    {
                                                        GUI.contentColor = staticColor;
                                                    }
                                                    if (GUILayout.Button(m.Name))
                                                    {
                                                        //m.Invoke(FindObjectOfType(currentObject.GetType()), null);
                                                    }
                                                    GUI.contentColor = Color.white;
                                                }
                                            }
                                        }
                                        GUILayout.EndScrollView();
                                    }
                                    GUILayout.EndVertical();
                                }
                                GUILayout.EndHorizontal();
                            }

                        }
                        else
                        {
                            GUILayout.Label("Select a type to begin");
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            }
                //private void onTypeViewer()
                //{
                //    GUILayout.BeginVertical("box", GUILayout.MinWidth(411), GUILayout.MaxWidth(800), GUILayout.ExpandWidth(true));
                //    {
                //        GUILayout.Label(currentType.type.Assembly.FullName.Substring(0, currentType.type.Assembly.FullName.IndexOf(',')) + " - " + currentType.name);
                //        GUILayout.BeginHorizontal();
                //        {
                //            GUILayout.BeginVertical(GUILayout.MinWidth(200), GUILayout.MaxWidth(400), GUILayout.ExpandWidth(true));
                //            {
                //                GUILayout.Label("Variables");
                //                variableSearch = GUILayout.TextField(variableSearch);
                //                variableScroll = GUILayout.BeginScrollView(variableScroll, "box");
                //                {
                //                    foreach (FieldInfo f in currentType.fields)
                //                    {
                //                        if (f.Name.ToLower().StartsWith(variableSearch.ToLower()) || f.Name.ToLower().Contains(variableSearch.ToLower()))
                //                        {
                //                            if (f.IsPublic)
                //                            {
                //                                GUI.contentColor = publicColor;
                //                            }
                //                            if (f.IsPrivate)
                //                            {
                //                                GUI.contentColor = privateColor;
                //                            }
                //                            if (f.IsStatic)
                //                            {
                //                                GUI.contentColor = staticColor;
                //                            }
                //                            if (GUILayout.Button(f.Name))
                //                            {
                //                                print(f.GetValue(FindObjectOfType(currentType.type)));
                //                                watchList.Add(new WatchItem(f));
                //                            }
                //                            GUI.contentColor = Color.white;
                //                        }
                //                    }
                //                }
                //                GUILayout.EndScrollView();
                //            }
                //            GUILayout.EndVertical();

                //            GUILayout.BeginVertical(GUILayout.MinWidth(200), GUILayout.MaxWidth(400), GUILayout.ExpandWidth(true));
                //            {
                //                GUILayout.Label("Methods");
                //                methodSearch = GUILayout.TextField(methodSearch);
                //                methodScroll = GUILayout.BeginScrollView(methodScroll, "box");
                //                {
                //                    foreach (MethodInfo m in currentType.methods)
                //                    {
                //                        if (m.Name.ToLower().StartsWith(methodSearch.ToLower()) || m.Name.ToLower().Contains(methodSearch.ToLower()))
                //                        {
                //                            if (GUILayout.Button(m.Name))
                //                            {
                //                                m.Invoke(FindObjectOfType(currentType.type), null);
                //                            }
                //                        }
                //                    }
                //                }
                //                GUILayout.EndScrollView();
                //            }
                //            GUILayout.EndVertical();
                //        }
                //        GUILayout.EndHorizontal();
                //    }
                //    GUILayout.EndVertical();
                //}
            private void onWatch(int height)
            {
                GUILayout.BeginHorizontal("box", GUILayout.Height(height));
                {
                    watchScroll = GUILayout.BeginScrollView(watchScroll);
                    {
                        if (watchList.Count > 0)
                        {
                            foreach (WatchItem w in watchList)
                            {
                                GUILayout.BeginHorizontal("box");
                                {
                                    GUILayout.BeginVertical(GUILayout.Width(150));
                                    {
                                        GUILayout.Label(w.field.FieldType.ToString());
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical(GUILayout.Width(200));
                                    {
                                        GUILayout.Label(w.field.Name.ToString());
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical(GUILayout.Width(375));
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
                        else
                        {
                            GUILayout.Label("No variables being watched, add some in the Explore menu");
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
                    GUILayout.Label("Execute will be here");
                }
                GUILayout.EndHorizontal();
            }
            private void onLogs(int height)
            {
                GUILayout.BeginHorizontal("box", GUILayout.Height(height));
                {
                    GUILayout.Label("Logs will be here");
                }
                GUILayout.EndHorizontal();
            }
            private void onMemory(int height)
            {
                GUILayout.BeginHorizontal("box", GUILayout.Height(height));
                {
                    GUILayout.Label("Memory will be here");
                }
                GUILayout.EndHorizontal();
            }
    }
}
   