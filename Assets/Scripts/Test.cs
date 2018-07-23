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
        return;

        Debug.Log("普通日志");
        Debug.LogError("普通错误");

        _msgId = PinnedLog.AddMsg("我是不会被清掉的日志");

        // 测试实例方法替换
        InstanceMethodTest InstanceTest = new InstanceMethodTest();
        InstanceTest.Test();

        // 测试属性替换
        PropertyHookTest propTest = new PropertyHookTest();
        propTest.Test();

        // 参数类型是私有类型的方法替换测试
        PrivateTypeArgMethodTest privateTypeArgMethodTest = new PrivateTypeArgMethodTest();
        privateTypeArgMethodTest.Test();
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

        return;
        PinnedLog.RemoveMsg(_msgId);
        PinnedLog.ClearAll();
    }
}
