using System;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental
{
    [Serializable]
    [CreateAssetMenuAttribute(fileName = "Task", menuName="Quest/Create Task")]
    public class Task : ScriptableObject
    {
        public delegate void StatusChanged(TaskStatus before, TaskStatus after, Task self);
        public delegate void ProgressChanged(float before, Task task);
        
        public event ProgressChanged OnProgressChanged;
        public event StatusChanged OnStatusChanged;

        [TextArea]
        public string Description;
        
        public bool autoComplete = false;
        
        public TaskRequirement Requirement = TaskRequirement.Required;
        
        public bool GiveRewardsOnTaskComplete;
        
        [Header("Task Objectives")]
        public String instanceGuid;
        
        public String prefabGuid;
        
        public bool IsSpecififc;
        
        public TutorialBlank tutorial;
        
        
        [SerializeField]
        private String guid = System.Guid.NewGuid().ToString();
        
        [SerializeField]
        private float requiredProgress = 1f;
        
        [NonSerialized]
        private bool gaveRewards;
        
        [NonSerialized]
        private float progress;
        
        [Header("Task objects to give")]
        [SerializeField]
        private List<PrefabSourceContainer> taskObjects = new List<PrefabSourceContainer>();
        
        [Header("Task Rewards")]
        [SerializeField]
        private List<Reward> rewards = new List<Reward>();
        
        [NonSerialized]
        private TaskStatus status = TaskStatus.InActive;
        
        public virtual Quest Owner 
        { 
            get; 
            set; 
        }
        public string Guid
        {
            get => guid;
            set => guid = value;
        }

        public float RequiredProgress
        {
            get => requiredProgress;
            set => requiredProgress = value;
        }

        public bool GaveRewards
        {
            get => gaveRewards;
            set => gaveRewards = value;
        }

        public float Progress
        {
            get => progress;
            set => progress = value;
        }

        public List<Reward> Rewards
        {
            get => rewards;
            set => rewards = value;
        }

        public TaskStatus Status
        {
            get => status;
            protected set
            {
                var before = status;
                status = value;

                if (before != status)
                {
                    OnStatusChanged?.Invoke(before, status, this);
                }
            }
        }
        
        public bool IsCompleted
        {
            get => Status == TaskStatus.Completed; 
        }

        #region Actions

        public bool ChangeProgress(float amount)
        {
            return SetProgress(Progress + amount);
        }
        
        public virtual bool SetProgress(float amount)
        {
            var before = progress;
            if (amount <= requiredProgress)
            {
                progress = amount;
            }

            if (Mathf.Approximately(before, progress) == false)
            {
                OnProgressChanged?.Invoke(before, this);
            }
            
            Debug.Log($"Выполнено: {Progress}/{RequiredProgress}");
            
            if (IsProgressSufficientToComplete())
            {
                Complete();
            }
            return true;
        }

        public virtual bool IsProgressSufficientToComplete()
        {
            return Progress >= RequiredProgress;
        }
        
        public bool CanComplete()
        {
            //InProgress
            return true;
        }
        
        public virtual bool Complete(bool forceComplete = true)
        {
            if (!CanComplete() && forceComplete == false)
            {
                return false;
            }

            Status = TaskStatus.Completed;

            if (GiveRewardsOnTaskComplete)
            {
                GiveTaskRewards();
            }
            
            Debug.Log("Task completed: " + Description);
            
            return IsCompleted;
        }
        
        //Экспериментальная выдача наград за задачу
        public void GiveTaskRewards()
        {
            if (!gaveRewards)
            {
                gaveRewards = true;
                foreach (var reward in Rewards)
                {
                    if (reward != null)
                    {
                        //Todo
                    }
                }
            }
        }
        
        public virtual void Fail()
        {
            if (Status != TaskStatus.Completed)
            {
                Status = TaskStatus.Failed;
            }
        }
        
        public virtual void Cancel()
        {
            ResetProgress();
        }
        
        public virtual void ResetProgress()
        {
            gaveRewards = false;
            SetProgress(0f);
            Status = TaskStatus.InActive;
        }
        
        public virtual void Activate()
        {
            Status = TaskStatus.Active;
        }

        #endregion
    }
}


