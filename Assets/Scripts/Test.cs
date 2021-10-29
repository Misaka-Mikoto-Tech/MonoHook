using DotNetDetour;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;


public class Test : MonoBehaviour
{
    public Button btn;
    public Text   txtInfo;
    public Text   txtTestVal;

    #region test case
#if ENABLE_HOOK_TEST_CASE

    private int _msgId;

    private void Awake()
    {
        btn.onClick.AddListener(OnBtnTestClick);
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

    public void OnBtnTestClick()
    {
        Debug.Log("Test Begin");

        // 实例方法替换测试
        {
            InstanceMethodTest InstanceTest = new InstanceMethodTest();
            InstanceTest.Test();

            int count = 2000;
            string str = $"InstanceMethodTest {count} times\r\n";
            DateTime dt = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                InstanceTest.Test();
            }
            str += $"patch:      {(int)new TimeSpan(DateTime.Now.Ticks - dt.Ticks).TotalMilliseconds} ms\r\n";

            dt = DateTime.Now;
            InstanceMethodTest.noPatch = true;
            for (int i = 0; i < count; i++)
            {
                InstanceTest.Test();
            }
            str += $"no patch: {(int)new TimeSpan(DateTime.Now.Ticks - dt.Ticks).TotalMilliseconds} ms";
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
        GameObject_SetActive_HookTest.Init();
        btn.gameObject.SetActive(false);
        btn.gameObject.SetActive(true);

        Debug.Log("Test End");
    }
#endif
    #endregion
}
