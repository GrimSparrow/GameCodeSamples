using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Experimental
{
    public sealed class QuestManager : MonoBehaviour, IDataSerialized
    {
        //Events
        public event Quest.StatusChanged OnQuestStatusChanged;
        public event Quest.TaskProgressChanged OnQuestTaskProgressChanged;
        public event Quest.TaskStatusChanged OnQuestTaskStatusChanged;
        
        
        public QuestDatabase QuestDatabase;
        
        
        private static QuestManager instance;
        
        private InventoryData inventory;
        
        private QuestsContainer QuestStates;
        
        [SerializeField]
        private EnvironmentController envController;
        
        [SerializeField]
        private QuestDashboard questDashboard;
        
        [Header("Quests that invoked at some time")]
        [SerializeField] 
        private List<TimedQuest> timedQuests;

        public static QuestManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<QuestManager>();
                }

                return instance;
            }
        }
        public List<Quest> Quests
        {
            get => QuestDatabase == null ? new List<Quest>() : QuestDatabase.Quests;
            set
            {
                if (QuestDatabase != null)
                {
                    QuestDatabase.Quests = value;
                }
            }
        }
        private void Start()
        {
            QuestDatabase.SetQuestsToInactive();
            Assert.IsNotNull(QuestDatabase, "Need to setup quest database!!!");
            
            QuestStates = new QuestsContainer();
            RegisterListeners();
            
            questDashboard = FindObjectOfType<QuestDashboard>();
        }

        private void RegisterListeners()
        {
            ParametersManager.DeathEvent += NpcDeath;
            BaseInteractiveItem.UsingInteractiveItemEvent += OnObjectUsed;
            ExploreTestEvent.ExploreEvent += OnExplore;
            
            PubSub.RegisterListeners<DialogueChoiceEvent>(OnDialogueDecision);
            PubSub.RegisterListeners<BuildEvent>(OnBlockBuilded);
        }

        private void UnregisterListeners()
        {
            ParametersManager.DeathEvent -= NpcDeath;
            BaseInteractiveItem.UsingInteractiveItemEvent -= OnObjectUsed;
            ExploreTestEvent.ExploreEvent -= OnExplore;
            
            PubSub.UnregisterListeners<DialogueChoiceEvent>(OnDialogueDecision);
            PubSub.UnregisterListeners<BuildEvent>(OnBlockBuilded);
            OnQuestTaskStatusChanged = null;
            OnQuestTaskStatusChanged = null;
        }

        private void OnDestroy()
        {
            UnregisterListeners();
        }

        public bool IsQuestTaskActive(String taskToComplete)
        {
            var tasks = QuestStates.ActiveQuests.Where(q =>
                q.Tasks.Any(t => t.instanceGuid == taskToComplete && !t.isCompleted));
            
            return tasks.Any() && IsTaskObjectsExists(tasks);
        }

        public bool IsTaskObjectsExists(IEnumerable<Quest> quests)
        {
            foreach (var quest in quests)
            {
                foreach (var task in quest.Tasks)
                {
                    foreach (var taskObject in task.taskObjects)
                    {
                        //TODO если инвентарь не содержит - отваливаем
                    }
                }
            }
            return true;
        }

        private void Update()
        {
            foreach (var timedQuest in timedQuests)
            {
                if (timedQuest.Minutes == envController.CurrentMinute & !HasActiveQuest(timedQuest.QuestToInvoke))
                {
                    timedQuest.QuestToInvoke.Activate();
                }
            }
        }

        public bool HasActiveQuest(Quest quest)
        {
            return QuestStates.ActiveQuests.Contains(quest);
        }

        public bool HasCompletedQuest(Quest quest)
        {
            return QuestStates.CompletedQuests.Contains(quest);
        }

        public void NotifyQuestTaskProgressChanged(float before, Task task, Quest quest)
        {
            OnQuestTaskProgressChanged?.Invoke(before, task, quest);
        }
        
        public void NotifyQuestTaskStatusChanged(TaskStatus before, TaskStatus after, Task task, Quest quest)
        {
            OnQuestTaskStatusChanged?.Invoke(before, task, quest);
        }

        public void RedrawDashboard()
        {
            questDashboard.RedrawDashboard(GetCurrentQuest());
        }
        
        public QuestsContainer GetQuestStates()
        {
            return QuestStates;
        }

        public void SetAllNotCurrent()
        {
            foreach (var quest in QuestStates.ActiveQuests)
            {
                quest.IsCurrent = false;
            }
        }

        public Quest GetCurrentQuest()
        {
            return QuestStates.ActiveQuests.FirstOrDefault(q => q.IsCurrent);
        }
        
        public void NotifyQuestStatusChanged(Quest quest)
        {
            if (QuestStates == null)
            {
                QuestStates = new QuestsContainer();
            }
            switch (quest.Status)
            {
                case QuestStatus.InActive:
                case QuestStatus.Cancelled:
                    QuestStates.ActiveQuests.Remove(quest);
                    break;
                case QuestStatus.Active:
                    QuestStates.ActiveQuests.Add(quest);
                    if (quest.setCurrentOnActivation)
                    {
                        SetQuestCurrent(quest);
                    }
                    break;
                case QuestStatus.Completed:
                    QuestStates.ActiveQuests.Remove(quest);
                    QuestStates.CompletedQuests.Add(quest);
                    RedrawDashboard();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            OnQuestStatusChanged?.Invoke(quest);
        }

        public void SetQuestCurrent(Quest quest)
        {
            var currentQuest = GetCurrentQuest();
            if ( currentQuest != null && quest != currentQuest)
            {
                SetAllNotCurrent();
                
                foreach (var indicator in GameObject.FindGameObjectsWithTag("QuestActivityIndicator"))
                {
                    indicator.SetActive(false);
                }
            }
            quest.IsCurrent = !quest.IsCurrent;
            ToggleWaypoints(quest);
        }

        private  void ToggleWaypoints(Quest quest)
        {
            questDashboard.ExpDrawDashboard(quest);
            WaypointManager.Instance.DeactivateWaypoints();
            foreach (var task in quest.Tasks.Where(t=>t.Status == TaskStatus.Active))
            {
                if (!task.IsProgressSufficientToComplete() && quest.IsCurrent)
                {
                    WaypointManager.Instance.ActivateWaypointsByTask(task);
                }
            }
        }
        
        private void OnExplore(InstanceModelContainer interactiveModel)
        {
            foreach (var quest in QuestStates.ActiveQuests)
            {
                quest.SetTaskObjective(interactiveModel);
            }
        }

        private void NpcDeath(String guid)
        {
            foreach (var quest in QuestStates.ActiveQuests)
            {
                quest.SetTaskObjectiveByGuid(guid);
            }
        }
        
        private void OnBlockBuilded(object publishedEvent)
        {
            BuildEvent e = publishedEvent as BuildEvent;
            foreach (var quest in QuestStates.ActiveQuests)
            {
                quest.SetTaskObjective(e.instanceModelContainer);
            }
        }
        
        private void OnObjectUsed(InteractiveItemUsingModel interactiveModel)
        {
            if (interactiveModel.Command != "talk")
            {
                foreach (var quest in QuestStates.ActiveQuests)
                {
                    quest.SetTaskObjectiveByGuid(interactiveModel.ItemGuid);
                }
            }
        }
        
        private void OnDialogueDecision(object publishedEvent)
        {
            DialogueChoiceEvent e = publishedEvent as DialogueChoiceEvent;
            foreach (var quest in QuestStates.ActiveQuests)
            {
                quest.SetTaskObjective(e._taskKey);
            }
        }


        #region  Save Logic

        public string Key => ModelKeyNames.QuestsDataKey;
        public BaseModel GetCurrentDataModel()
        {
            return QuestStates == null
                ? new QuestsDataModel()
                : new QuestsDataModel()
                {
                    ActiveQuests = ToSaveModel(QuestStates.ActiveQuests),
                    CompletedQuests = ToSaveModel(QuestStates.CompletedQuests)
                };
        }

        public void SetCurrentModel(BaseModel model)
        {
            var castedModel = ModelKeyNames.CastModelToTargetType<QuestsDataModel>(model);

            LoadActiveQuests(castedModel.ActiveQuests);
            LoadCompletedQuests(castedModel.CompletedQuests);
        }
        
        private void LoadActiveQuests(List<SavedQuestState> savedQuests)
        {
            if (QuestStates == null)
            {
                QuestStates = new QuestsContainer();
            }
            QuestStates.ActiveQuests.Clear();
            foreach (var savedQuest in savedQuests)
            {
                QuestStates.ActiveQuests.Add(savedQuest.RestoreQuest());
            }

            RedrawDashboard();
        }
        
        private void LoadCompletedQuests(List<SavedQuestState> savedQuests)
        {
            if (QuestStates == null)
            {
                QuestStates = new QuestsContainer();
            }
            QuestStates.CompletedQuests.Clear();
            foreach (var savedQuest in savedQuests)
            {
                QuestStates.CompletedQuests.Add(savedQuest.RestoreQuest());
            }
        }
        
        private List<SavedQuestState> ToSaveModel(HashSet<Quest> quests)
        {
            List<SavedQuestState> result = new List<SavedQuestState>();
            if (quests == null || !quests.Any())
            {
                result =  new List<SavedQuestState>();
            }
            else
            {
                result.AddRange(quests.Select(quest => new SavedQuestState(quest)));
            }

            return result;
        }
        
        #endregion

    }
    
    
    #region Save Models
    public class QuestsDataModel : BaseModel
    {
        public List<SavedQuestState> ActiveQuests { get; set; }
        public List<SavedQuestState> CompletedQuests { get; set; }

        public QuestsDataModel()
        {
            ActiveQuests = new List<SavedQuestState>();
            CompletedQuests = new List<SavedQuestState>();
        }
    }

    public class SavedTaskState
    {
        public string taskGuid;
        
        public float progress;

        public SavedTaskState()
        {
            
        }
        public SavedTaskState(Task task)
        {
            taskGuid = task.Guid;
            progress = task.Progress;
        }
    }

    public class SavedQuestState
    {
        public string questGuid;
        
        public QuestStatus questStatus;
        
        public bool isCurrent;
        
        public List<SavedTaskState> tasks = new List<SavedTaskState>();

        public SavedQuestState()
        {
            
        }
        public SavedQuestState(Quest quest)
        {
            questGuid = quest.QuestGuid;
            questStatus = quest.Status;
            isCurrent = quest.IsCurrent;
            foreach (var task in quest.Tasks)
            {
                tasks.Add(new SavedTaskState(task));
            }
        }


        public Quest RestoreQuest()
        {
            var databaseQuest = QuestManager.Instance.QuestDatabase.GetQuestByGuid(questGuid);
            if (databaseQuest == null)
            {
                return null;
            }

            databaseQuest.Status = questStatus;
            databaseQuest.IsCurrent = isCurrent;

            //var QuestTasks = new List<Task>();

            foreach (var task in tasks)
            {
                var qtask = databaseQuest.Tasks.First(t => t.Guid == task.taskGuid);
                qtask.Progress = task.progress;
                //QuestTasks.Add(qtask);
            }
            //databaseQuest.Tasks = QuestTasks;
            return databaseQuest;
        }
    }
    #endregion
}

