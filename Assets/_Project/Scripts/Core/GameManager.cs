using System;
using PrisonLife.Config;
using UnityEngine;

namespace PrisonLife.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [SerializeField] private int startingCash = 0;

        public GameConfig Config => config;

        public event Action<int> OnCashChanged;
        public event Action<string, bool> OnUpgradeChanged; // id, purchased

        private int _cash;
        public int Cash => _cash;

        private readonly System.Collections.Generic.HashSet<string> _purchased = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _cash = startingCash;
        }

        private void Start()
        {
            OnCashChanged?.Invoke(_cash);
        }

        public void AddCash(int amount)
        {
            if (amount == 0) return;
            _cash = Mathf.Max(0, _cash + amount);
            OnCashChanged?.Invoke(_cash);
        }

        public bool TrySpendCash(int amount)
        {
            if (amount <= 0) return true;
            if (_cash < amount) return false;
            _cash -= amount;
            OnCashChanged?.Invoke(_cash);
            return true;
        }

        public int SpendUpTo(int amount)
        {
            int spent = Mathf.Min(_cash, Mathf.Max(0, amount));
            if (spent > 0)
            {
                _cash -= spent;
                OnCashChanged?.Invoke(_cash);
            }
            return spent;
        }

        public bool IsPurchased(string id) => _purchased.Contains(id);

        public void MarkPurchased(string id)
        {
            if (_purchased.Add(id))
                OnUpgradeChanged?.Invoke(id, true);
        }
    }

    public static class UpgradeIds
    {
        public const string Drill = "drill";
        public const string Vehicle = "vehicle";
        public const string Workers = "workers";
        public const string JailExpand = "jail_expand";
        public const string Police = "police";
    }
}
