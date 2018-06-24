using UnityEngine;
using UnityEngine.UI;


public class Test : MonoBehaviour
{
    public Button btn;

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

        // 测试实例方法替换
        InstanceMethodTest test = new InstanceMethodTest();
        test.Test();
    }

    public void OnBtnClick()
    {
        PinnedLog.RemoveMsg(_msgId);
        PinnedLog.ClearAll();
    }
}
