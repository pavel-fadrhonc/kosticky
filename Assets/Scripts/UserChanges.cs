using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DefaultNamespace
{
    [Serializable]
    public class UserChanges
    {
        [Serializable]
        public class ChangeAdd
        {
            public Vector3 WorldPos;
            public int biomeIdx;
        }

        [Serializable]
        public class ChangeRemove
        {
            public Vector3 WorldPos;
        }
        
        [SerializeField][HideInInspector]
        private List<ChangeAdd> _addChanges = new List<ChangeAdd>();
        public IReadOnlyList<ChangeAdd> AddChanges => _addChanges;
        
        [SerializeField][HideInInspector]
        private List<ChangeRemove> _removeChanges = new List<ChangeRemove>();
        public IReadOnlyList<ChangeRemove> RemoveChanges => _removeChanges;

        private string _saveFilePath;

        public void Init()
        {
            _saveFilePath = Application.persistentDataPath + Path.DirectorySeparatorChar + "UserChanges.txt";
        }

        public void RecordAddChange(Vector3 worldPos_, int biomeIndex_)
        {
            _addChanges.Add(new ChangeAdd()
            {
                WorldPos = worldPos_,
                biomeIdx = biomeIndex_
            });
        }

        public void RecordRemoveChange(Vector3 worldPos_)
        {
            _removeChanges.Add(new ChangeRemove()
            {
                WorldPos = worldPos_
            });
        }

        /// <summary>
        /// Removes all changes added or loaded
        /// </summary>
        public void Reset()
        {
            _addChanges.Clear();
            _removeChanges.Clear();
        }

        public void Save()
        {
            var jsonString = JsonUtility.ToJson(this);
            
            if (!File.Exists(_saveFilePath))
            {
                File.Create(_saveFilePath).Close();
            }

            File.WriteAllText(_saveFilePath, jsonString);
        }

        public void Load()
        {
            if (!File.Exists(_saveFilePath))
                return;

            var content = File.ReadAllText(_saveFilePath);
            var changes = JsonUtility.FromJson<UserChanges>(content);

            if (changes == null)
                return;

            _addChanges = changes._addChanges;
            _removeChanges = changes._removeChanges;
        }
    }
}