using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using KSP.IO;

namespace Kreeper
{
    public class AssemblyObject
    {
        public string name;
        public string version;
        public List<Type> types;

        public AssemblyObject(string n, string v, List<Type> t)
        {
            name = n;
            version = v;
            types = t;
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class Kreeper : MonoBehaviour
    {
        Rect window = new Rect(0, 0, 500, 500);
        List<AssemblyObject> assemblies = new List<AssemblyObject>();

        string assemblySearch = "";
        Vector2 assemblyScroll = new Vector2(0, 0);
        int selectedAssembly = 0;

        string typeSearch = "";
        Vector2 typeScroll = new Vector2(0, 0);

        private void Start()
        {
            UnityEngine.Object.DontDestroyOnLoad(this);

            using (IEnumerator<AssemblyLoader.LoadedAssembly> enumerator = AssemblyLoader.loadedAssemblies.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    AssemblyLoader.LoadedAssembly current = enumerator.Current;

                    string name = current.assembly.FullName.Substring(0, current.assembly.FullName.IndexOf(','));
                    string version = current.assembly.FullName.Substring(current.assembly.FullName.IndexOf(','), current.assembly.FullName.IndexOf(',',current.assembly.FullName.IndexOf(',')));
                    List<Type> types = new List<Type>();
                    foreach (Type t in current.assembly.GetTypes())
                    {
                        types.Add(t);
                    }

                    assemblies.Add(new AssemblyObject(name, version, types));
                }
            }
        }
        private void Awake()
        {

        }
        private void Update()
        {

        }
        private void OnGUI()
        {
            window = GUILayout.Window(1, window, onWindow, "Kreeper");
        }
        private void onWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(150));
                {
                    GUILayout.Label("Assemblies");
                    assemblySearch = GUILayout.TextField(assemblySearch);
                    assemblyScroll = GUILayout.BeginScrollView(assemblyScroll, "box");
                    {
                        GUILayout.BeginVertical();
                        {
                            List<string> contents = new List<String>();
                            foreach (AssemblyObject a in assemblies)
                            {
                                if (a.name.ToLower().StartsWith(assemblySearch.ToLower()))
                                {
                                    contents.Add(a.name);
                                }
                            }
                            int prev = selectedAssembly;
                            if (selectedAssembly > contents.Count)
                            {
                                selectedAssembly = 0;
                            }
                            selectedAssembly = GUILayout.SelectionGrid(selectedAssembly, contents.ToArray(), 1);
                            if (selectedAssembly != prev)
                            {
                                typeScroll.y = 0;
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(300));
                {
                    GUILayout.Label("Types");
                    typeSearch = GUILayout.TextField(typeSearch);
                    typeScroll = GUILayout.BeginScrollView(typeScroll, "box");
                    {
                        GUILayout.BeginVertical();
                        {
                            List<Type> contents = new List<Type>();
                            foreach (Type t in assemblies[selectedAssembly].types)
                            {
                                if (t.Name.ToLower().StartsWith(typeSearch.ToLower()))
                                {
                                    contents.Add(t);
                                    if (GUILayout.Button(t.Name))
                                    {
                                        onTypeButton(t);
                                    }
                                }
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void onTypeButton(Type t)
        {
            //THIS IS WHERE IM MESSING AROUND
            object[] o = FindObjectsOfType(t);
            FieldInfo f = t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)[0];
            print(f.ToString());
        }
    }
}
