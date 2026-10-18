using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public abstract class BasePanel : MonoBehaviour 
{
    // 用于控制面板透明度的组件
    private CanvasGroup canvasGroup;
    // 淡入淡出的速度
    private float alphaSpeed = 10;
    // 当前隐藏还是显示
    public bool isShow = false;

    private UnityAction hideCallBack = null;
    protected virtual void Awake()
    {
        canvasGroup = this.GetComponent<CanvasGroup>();
        if (canvasGroup == null )
            canvasGroup = this.AddComponent<CanvasGroup>();
          
    }
    protected virtual void Start()
    {
        Init();
    }

    public abstract void Init();

    public virtual void ShowMe()
    {
        canvasGroup.alpha = 0;
        isShow = true;
    }

    public virtual void HideMe( UnityAction callBack )
    {
        canvasGroup.alpha = 1;
        isShow = false;
        hideCallBack = callBack;
    }
    
    
    // Update is called once per frame
    protected virtual void Update()
    {
        if( isShow && canvasGroup.alpha != 1)
        {
            canvasGroup.alpha += alphaSpeed * Time.deltaTime;
            if ( canvasGroup.alpha >= 1)
            {
                canvasGroup.alpha = 1;
            }
        }
        else if(!isShow && canvasGroup.alpha != 0)
        {
            canvasGroup.alpha -= alphaSpeed * Time.deltaTime;
            if (canvasGroup.alpha <= 0)
            {
                canvasGroup.alpha = 0;
                hideCallBack?.Invoke();
                
            }
        }
    }

    
}
