using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Experimental
{
    [Serializable]
    [CreateAssetMenuAttribute(fileName = "Quest", menuName="Quest/Create Quest")]
    public class Quest : ScriptableObject
    {
        public delegate void StatusChanged(Quest quest);
        public delegate void TaskReachedTimeLimit(Task task, Quest quest);
        public delegate void TaskStatusChanged(TaskStatus before, Task task, Quest quest);
        public delegate void TaskProgressChanged(float taskProgressBefore, Task task, Quest quest);

        public event StatusChanged OnStatusChanged;
        public event TaskProgressChanged OnTaskProgressChanged;
        public event TaskStatusChanged OnTaskStatusChanged;
        

        public String QuestGuid = Guid.NewGuid().ToString();

        public string Name;
        
        [TextArea]
        public string Description;
        
        public Sprite questImage;
        
        public TaskOrder taskOrder = TaskOrder.Parallel;
        
        public bool setCurrentOnActivation;
        
        public int maxRepeatTimes = 1;
        
        public IQuestCondition[] conditions = new IQuestCondition[0];

        [SerializeField]
        private bool autoCompleteWhenTasksAreDone = false;
        
        [SerializeField]
        private QuestStatus status = QuestStatus.InActive;

        [SerializeField]
        private List<Task> tasks = new List<Task>();

        [Header("Quest Conditions")]
        [SerializeField]
        private List<Quest> RequiresFinishedQuests = new List<Quest>();

        [Header("Quest Rewards")] 
        [SerializeField]
        private Reward reward;
        

        private bool isCurrent;
        
        private int repeatedTimes;

        public bool IsCurrent
        {
            get => isCurrent;
            set => isCurrent = value;
        }
        
        public List<Task> Tasks
        {
            get => tasks;
            set
            {
                tasks = value;
                RegisterEventsOnTasks();
                foreach (var task in tasks)
                {
                    task.Owner = this;
                }
            }
        }
        
        public QuestStatus Status
        {
            get => status;
            set
            {
                var before = status;
                status = value;

                if (before != status)
                {
                    NotifyQuestStatusChanged();
                }
            }
        }

        public int RepeatedTimes
        {
            get =>repeatedTimes; 
            set => repeatedTimes = value;
        }
        
        public Reward Reward
        {
            get => reward;
            set => reward = value;
        }
        
        
        private void OnDestroy()
        {
            status = QuestStatus.InActive;
        }
        
        #region Notifies
        public void NotifyQuestStatusChanged()
        {
            DoNotifyQuestStatusChanged();
        }
        protected virtual void DoNotifyQuestStatusChanged()
        {
            QuestManager.Instance.NotifyQuestStatusChanged(this);
            OnStatusChanged?.Invoke(this);
        }
        
        public void NotifyTaskStatusChanged(TaskStatus before, TaskStatus after, Task task)
        {
            DoNotifyTaskStatusChanged(before, after, task);
            
            if (after == TaskStatus.Completed)
            {
                if (taskOrder == TaskOrder.Single && Tasks.Any(t=> t.Status == TaskStatus.Active))
                {
                    return;
                }
                var nextTask = Tasks.FirstOrDefault(t => t.Status == TaskStatus.InActive);
                if (nextTask != null)
                {
                    nextTask.Activate();
                }
            }
        }
        
        protected virtual void DoNotifyTaskStatusChanged(TaskStatus before, TaskStatus after, Task task)
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.NotifyQuestTaskStatusChanged(before, after, task, this);
            }

            OnTaskStatusChanged?.Invoke(before, task, this);
        }

        public void NotifyTaskProgressChanged(float before, Task task)
        {
            
            DoNotifyTaskProgressChanged(before, task);
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.RedrawDashboard();
            }
            
            if (autoCompleteWhenTasksAreDone)
            {
                CompleteAndGiveRewards();
            }
        }
        
        protected virtual void DoNotifyTaskProgressChanged(float before, Task task)
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.NotifyQuestTaskProgressChanged(before, task, this);
            }

            OnTaskProgressChanged?.Invoke(before, task, this);
        }
        
        private void NotifyTasksQuestCompleted()
        {
            foreach (var task in Tasks)
            {
                task.NotifyQuestCompleted();
            }
        }
        
        #endregion

        #region Prepare Tasks
        private void RegisterEventsOnTasks()
        {
            foreach (var task in Tasks)
            {
                task.OnProgressChanged += NotifyTaskProgressChanged;
                task.OnStatusChanged += NotifyTaskStatusChanged;
            }
        }
        
        private void UnRegisterEventsOnTasks()
        {
            foreach (var task in Tasks)
            {
                task.OnProgressChanged -= NotifyTaskProgressChanged;
                task.OnStatusChanged -= NotifyTaskStatusChanged;
            }
        }

        private void AddOwnerToTasks()
        {
            foreach (var task in Tasks)
            {
                task.Owner = this;
            }
        }
        #endregion


        #region Actions
        
        public void ForceSetStatus(QuestStatus status)
        {
            IsCurrent = false;
            Status = status;
            RepeatedTimes = 0;
        }
        
        public void SetTaskObjective(string taskToComplete)
        {
            if (!string.IsNullOrEmpty(taskToComplete))
            {
                var tasks = Tasks.Where(t => t.instanceGuid == taskToComplete).ToList();
                foreach (var task in tasks)
                {
                    task.ChangeProgress(1);
                }
            }

            AutoComplete();
        }
        public void SetTaskObjective(InstanceModelContainer container)
        {
            var tasks = Tasks.Where(t => t.IsSpecififc ? t.instanceGuid == container.Guid : (container.PrefabContainer != null && t.prefabGuid == container.PrefabContainer.PrefabGuid)).ToList();
            foreach (var task in tasks)
            {
                task.ChangeProgress(1);
            }

            AutoComplete();
        }
        
        public void SetTaskObjectiveByGuid(string guid)
        {
            var tasks = Tasks.Where(t => t.instanceGuid ==  guid).ToList();
            foreach (var task in tasks)
            {
                task.ChangeProgress(1);
            }

            AutoComplete();
        }

        public void AutoComplete()
        {
            if (autoCompleteWhenTasksAreDone)
            {
                CompleteAndGiveRewards();
            }
        }
        
        public bool Activate(bool resetTaskProgress = true)
        {
            if (CanActivate() == false)
            {
                return false;
            }

            if (resetTaskProgress)
            {
                foreach (var task in Tasks)
                {
                    task.ResetProgress();
                }
            }

            Status = QuestStatus.Active;
            
            RegisterEventsOnTasks();
            AddOwnerToTasks();
            ActivateTasks();
            
            Debug.Log($"Взято задание {Name}");
            
            return true;
        }
        
        private void ActivateTasks()
        {
            switch (taskOrder)
            {
                case TaskOrder.Parallel:
                    foreach (var task in Tasks)
                    {
                        task.Activate();
                    }
                    break;
                case TaskOrder.Single:
                    if (Tasks.Count > 0)
                    {
                        Tasks[0].Activate();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public bool IsAllRequiredTasksCompleted()
        {
            return Tasks.All(task => (task.IsProgressSufficientToComplete() || task.Requirement != TaskRequirement.Required) 
                                     && (task.isCompleted || task.Requirement != TaskRequirement.Required));
        }

        public bool CanComplete()
        {
            return Status == QuestStatus.Active && IsAllRequiredTasksCompleted();
        }

        public bool CanGiveReward()
        {
            if (Reward.CanGive()) 
                return true;
            
            Debug.Log("Инветарь полон!");
            return false;
        }
        
        public bool CompleteAndGiveRewards(bool forceComplete = false)
        {
            if ((!CanComplete() && forceComplete == false) || !CanGiveReward())
            {
                return false;
            }

            RepeatedTimes++;

            //CompleteCompletableTasks(forceComplete);
            GiveRewards();

            Status = QuestStatus.Completed;
            
            Debug.Log($"Задание {Name} завершено!");
            
            NotifyTasksQuestCompleted();
            
            return true;
        }
        
        private void CompleteCompletableTasks(bool forceComplete)
        {
            foreach (var task in Tasks.Where(task => !task.isCompleted || forceComplete))
            {
                task.Complete(forceComplete);
            }
        }
        
        private void GiveRewards()
        {
            Reward.GiveReward();
            // пока не будем использовать
            foreach (var task in Tasks)
            {
                if (task.isCompleted)
                {
                    //task.GiveTaskRewards();
                }
            }
        }
        
        public virtual bool Cancel()
        {
            Status = QuestStatus.Cancelled;
            CancelAllTasks();

            return true;
        }

        private void CancelAllTasks()
        {
            foreach (var task in Tasks)
            {
                task.Cancel();
            }
        }
        
        public bool CanActivate()
        {
            foreach (var quest in RequiresFinishedQuests)
            {
                if (!QuestManager.Instance.HasCompletedQuest(quest))
                {
                    return false;
                }
            }

            if (QuestManager.Instance.HasActiveQuest(this))
            {
                return false;
            }

            if (RepeatedTimes + 1 > maxRepeatTimes)
            {
                return false;
            }

            foreach (var condition in conditions)
            {
                var canActivate = condition.CanActivateQuest(this);
                if (!canActivate)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}

