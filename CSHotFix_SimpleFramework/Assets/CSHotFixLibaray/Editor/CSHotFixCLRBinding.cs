﻿/*
* LCL support c# hotfix here.
*Copyright(C) LCL.All rights reserved.
* URL:https://github.com/qq576067421/cshotfix 
*QQ:576067421 
* QQ Group: 673735733 
 * Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at 
*  
* Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. 
*/
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine.AI;
using UnityEngine.Rendering;
using System.Linq;

[System.Reflection.Obfuscation(Exclude = true)]
public class CSHotFixCLRBinding
{
    [MenuItem("CSHotFix/GenMonoType")]
    static void GenerateCLRBinding()
    {
        if (!EditorUtility.DisplayDialog("警告", "你是否需要重新生成绑定信息？", "需要", "按错了"))
        {
            return;
        }
        List<Type> types = new List<Type>();
        types.Add(typeof(int));
        types.Add(typeof(float));
        types.Add(typeof(long));
        types.Add(typeof(object));
        types.Add(typeof(string));
        types.Add(typeof(Array));
        types.Add(typeof(Vector2));
        types.Add(typeof(Vector3));
        types.Add(typeof(Quaternion));
        types.Add(typeof(GameObject));
        types.Add(typeof(UnityEngine.Object));
        types.Add(typeof(Transform));
        types.Add(typeof(RectTransform));
        types.Add(typeof(Time));
        types.Add(typeof(Debug));
        //types.Add(typeof(UIEventListener));
        //所有DLL内的类型的真实C#类型都是ILTypeInstance
        types.Add(typeof(List<CSHotFix.Runtime.Intepreter.ILTypeInstance>));
        types.AddRange( AddGameDllTypes());
        types.AddRange(AddUnityDll());

        CSHotFix.Runtime.CLRBinding.BindingCodeGenerator.GenerateBindingCode(types, "Assets/CSHotFixLibaray/Generated/CLRGen");
        AddCSHotFixDefine();
        AssetDatabase.Refresh();

    }
    static List<string> GetDefineSymbols()
    {
#if UNITY_IPHONE
        string symbolsDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iPhone);
#elif UNITY_ANDROID
        string symbolsDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
#else
        string symbolsDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#endif
        return symbolsDefines.Split(';').ToList();
    }
    static void AddCSHotFixDefine()
    {
        var definesList = GetDefineSymbols();
        if (!definesList.Contains("CSHotFix"))
        {
            definesList.Add("CSHotFix");
        }
        string defineSymbols = string.Join(";", definesList.ToArray());
#if UNITY_IPHONE
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iPhone, defineSymbols);
#elif UNITY_ANDROID
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defineSymbols);
#else
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebPlayer, defineSymbols);
#endif
    }


    static List<Type> AddUnityDll()
    {
        List<Type> _outTypes = new List<Type>();
        _outTypes.AddRange(Assembly.Load("UnityEngine").GetTypes());
        _outTypes.AddRange(Assembly.Load("UnityEngine.UI").GetTypes());



        List<Type> outTypes = new List<Type>();
        foreach (var t in _outTypes)
        {
            if (FilterCommon(t))
            {
                continue;
            }
            //进行其他过滤，例如和移动平台不相干的、不适合的，和版本不相符的，以及其他不支持的。
            if (GenConfig.blackNamespaceList.Exists((_black) =>
            {
                if(t.Namespace!= null)
                {
                    return t.Namespace.Contains(_black);
                }
                else
                {
                    return false;
                }

            }))
            {
                continue;
            }
            if (GenConfig.blackTypeList.Exists((_black)=>{return t == _black; }))
            {
                continue;
            }

            outTypes.Add(t);
            
        }
        return outTypes;
    }

    static bool FilterCommon(Type t)
    {
        if(t.IsNotPublic || !t.IsPublic)
        {
            return true;
        }
        if(t.IsGenericType)
        {
            return true;
        }
        if (t.BaseType == typeof(Delegate) || t.BaseType == typeof(MulticastDelegate))
        {
            return true;
        }
        if (t.Name.Contains("<"))
        {
            return true;
        }
        //if (t.IsEnum)
        //{
        //    return true;
        //}
        return false;
    }

    static List<Type> AddGameDllTypes()
    {
        List<Type> outTypes = new List<Type>();
        Type[] _types = Assembly.Load("Assembly-CSharp").GetTypes();
        foreach(var t in _types)
        {
            var attr = t.GetCustomAttributes(false).ToList().Find((obj) => { return obj is CSHotFixMonoTypeExportAttribute; }) as CSHotFixMonoTypeExportAttribute;
            if (attr != null)
            {
                if (attr.ExportFlag == CSHotFixMonoTypeExportFlagEnum.Export)
                {
                    outTypes.Add(t);
                }
            }
            else
            {
                if (t.Namespace == null)
                {
                    continue;
                }
                else
                {
                    if (t.Namespace.Contains("LCL") ||
                        t.Namespace.Contains("UnityUI") ||
                        t.Namespace.Contains("GameDll")
                        )
                    {
                        if (FilterCommon(t))
                        {
                            continue;
                        }
                        outTypes.Add(t);
                    }
                }

            }
        }
        return outTypes;
    }
}
#endif
