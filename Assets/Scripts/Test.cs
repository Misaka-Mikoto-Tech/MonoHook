using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;


public class Test : MonoBehaviour
{
    public Button btn;
    public Text   txtInfo;

    private int _msgId;

    private void Awake()
    {
        btn.onClick.AddListener(OnBtnClick);
    }
    private void Start()
    {
        Debug.Log("普通日志");
        Debug.LogError("普通错误");

        _msgId = PinnedLog.AddMsg("我是不会被清掉的日志");

        // 实例方法替换测试
        InstanceMethodTest InstanceTest = new InstanceMethodTest();
        InstanceTest.Test();

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
        btn.gameObject.SetActive(false);
        btn.gameObject.SetActive(true);
    }

    public void OnBtnClick()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("pointer size:{0}\r\n", System.IntPtr.Size);
        sb.AppendFormat("operation name:{0}\r\n", SystemInfo.operatingSystem);
        sb.AppendFormat("processorType:{0}\r\n", SystemInfo.processorType);
        sb.AppendLine();
        txtInfo.text = sb.ToString();

        // 测试实例方法替换
        InstanceMethodTest InstanceTest = new InstanceMethodTest();
        sb.Length = 0;
        string info = InstanceTest.Test();
        sb.AppendLine(info);
        txtInfo.text += sb.ToString();

        PinnedLog.RemoveMsg(_msgId);
        PinnedLog.ClearAll();
    }
}
