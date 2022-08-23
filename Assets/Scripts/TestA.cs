using DotNetDetour;
using MonoHook.Test;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MonoHook
{
    public unsafe class TestA   : MonoBehaviour
    {
        public Button btn, btnDelFile;
        public Text txtInfo;
        public Text txtTestVal;

        #region test case
#if ENABLE_HOOK_TEST_CASE

        private int _msgId;

        private void Awake()
        {
            btn.onClick.AddListener(OnBtnTestClick);
            btnDelFile.onClick.AddListener(OnBtnTestDelFileClick);
        }
        private void Start()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("pointer size:{0}\r\n", System.IntPtr.Size);
            sb.AppendFormat("is IL2CPP:{0}\r\n", LDasm.IsIL2CPP());
            sb.AppendFormat("operation name:{0}\r\n", SystemInfo.operatingSystem);
            sb.AppendFormat("processorType:{0}\r\n", SystemInfo.processorType);
            sb.AppendLine();
            txtInfo.text = sb.ToString();
        }

        public void OnBtnTestDelFileClick()
        {
            string fileName = "test.txt";
            string dirPath = "Assets/test_dir";
            string filePath1 = $"Assets/{fileName}";
            string filePath2 = $"{dirPath}/{fileName}";
            {
                File.Create(filePath1).Close();
                File.Delete(filePath1);
            }
            {
                Directory.CreateDirectory(dirPath);
                File.Create(filePath2).Close();
                Directory.Delete(dirPath, true);
            }
            {
                File.Create(filePath1).Close();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                AssetDatabase.DeleteAsset(filePath1);
            }
        }

        public void OnBtnTestClick()
        {
            Debug.Log("Test Begin");

            // 实例方法替换测试
            {
                InstanceMethodTest.InstallPatch();
                InstanceMethodTest.callOriFunc = true;
                InstanceMethodTest InstanceTest = new InstanceMethodTest();
                InstanceTest.Test();
                int ret = InstanceTest.Test();

                int count = 10 * 1000 * 1000;
                string str = $"InstanceMethodTest {count} times, ret: {ret}\r\n";

                DateTime dt = DateTime.Now;
                long val = 0;
                {// patch and call original function
                    for (int i = 0; i < count; i++)
                    {
                        val += InstanceTest.Test();
                    }
                    str += $"patch:      {(int)new TimeSpan(DateTime.Now.Ticks - dt.Ticks).TotalMilliseconds} ms, ret:{val}\r\n";
                }

                {// patch but dont call original function
                    dt = DateTime.Now;
                    val = 0;
                    InstanceMethodTest.callOriFunc = false;
                    for (int i = 0; i < count; i++)
                    {
                        val += InstanceTest.Test();
                    }
                    str += $"patch, no ori call:{(int)new TimeSpan(DateTime.Now.Ticks - dt.Ticks).TotalMilliseconds} ms, ret:{val}\r\n";
                }

                {// no patch
                    InstanceMethodTest.UninstallPatch();
                    dt = DateTime.Now;
                    val = 0;
                    for (int i = 0; i < count; i++)
                    {
                        val += InstanceTest.Test();
                    }
                    str += $"no patch: {(int)new TimeSpan(DateTime.Now.Ticks - dt.Ticks).TotalMilliseconds} ms, ret:{val}";
                }

                txtTestVal.text = str;
            }

            // 属性替换测试
            PropertyHookTest propTest = new PropertyHookTest();
            propTest.Test();

            // 参数类型是私有类型的方法替换测试
            PrivateTypeArgMethodTest privateTypeArgMethodTest = new PrivateTypeArgMethodTest();
            privateTypeArgMethodTest.Test();

            // 构造函数替换测试
            CtorHookTest ctorHookTest = new CtorHookTest();
            ctorHookTest.Test();

            // 测试GameObject.SetActive
            GameObject_SetActive_HookTest.Test(btn.gameObject);

            {// 泛型方法 hook 测试，目前仅支持 il2cpp
                GenericMethod_HookTest.Install();

                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Instantiate((UnityEngine.Object)go);

                GameObject goNew = Instantiate<UnityEngine.Object>(go) as GameObject;
                goNew.name = "hook_go_Instantiate";
                goNew.transform.localPosition = new Vector3(-5f, 0, 0);
                Destroy(go);
                Destroy(goNew);
            }
            Debug.Log("Test End");
        }
#endif
        #endregion
    }
}

