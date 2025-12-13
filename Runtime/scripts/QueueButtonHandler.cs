・ｿusing UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine;
using UnityEngine.UI;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// UI繝懊ち繝ｳ縺九ｉQueueManager縺ｮ繝｡繧ｽ繝・ラ繧貞他縺ｳ蜃ｺ縺吶◆繧√・繝悶Μ繝・ず繧ｹ繧ｯ繝ｪ繝励ヨ縺ｧ縺吶・    /// Button繧ｳ繝ｳ繝昴・繝阪Φ繝医°繧臥峩謗･UdonSharp縺ｮ繝｡繧ｽ繝・ラ繧貞他縺ｳ蜃ｺ縺吶◆繧√↓菴ｿ逕ｨ縺励∪縺吶・    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QueueButtonHandler : UdonSharpBehaviour
    {
        [SerializeField] private QueueManager queueManager;

        /// <summary>
        /// 蠕・ｩ溷・縺ｫ蜈･繧・謚懊￠繧九ｒ繝医げ繝ｫ縺励∪縺吶・        /// Unity Button 縺ｮ OnClick() 縺九ｉ蜻ｼ縺ｳ蜃ｺ縺輔ｌ縺ｾ縺吶・        /// </summary>
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
        /// 繧ｪ繝ｼ繝翫・縺梧ｬ｡縺ｮ繝励Ξ繧､繝､繝ｼ繧貞他縺ｳ蜃ｺ縺励∪縺吶・        /// </summary>
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
