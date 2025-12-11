using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// UIボタンからQueueManagerのメソッドを呼び出すためのブリッジスクリプトです。
    /// Buttonコンポーネントから直接UdonSharpのメソッドを呼び出すために使用します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QueueButtonHandler : UdonSharpBehaviour
    {
        [SerializeField] private QueueManager queueManager;

        /// <summary>
        /// 待機列に入る/抜けるをトグルします。
        /// Unity Button の OnClick() から呼び出されます。
        /// </summary>
        public void OnToggleButtonClick()
        {
            if (queueManager != null)
            {
                queueManager.ToggleQueue();
            }
            else
            {
                Debug.LogError("[QueueButtonHandler] QueueManager reference is missing!");
            }
        }

        /// <summary>
        /// オーナーが次のプレイヤーを呼び出します。
        /// </summary>
        public void OnAdvanceButtonClick()
        {
            if (queueManager != null)
            {
                queueManager.AdvanceQueue();
            }
            else
            {
                Debug.LogError("[QueueButtonHandler] QueueManager reference is missing!");
            }
        }
    }
}
