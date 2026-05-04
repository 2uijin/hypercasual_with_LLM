using System;
using PrisonLife.FX;
using UnityEngine;

namespace PrisonLife.Quarry
{
    /// <summary>
    /// Single rock. Mined in one hit; owner spawner handles respawn timing.
    /// </summary>
    public class Rock : MonoBehaviour
    {
        public event Action<Rock> OnMined;

        [Header("SFX")]
        [SerializeField] private AudioClip breakSfx;
        [SerializeField, Range(0f, 1f)] private float breakSfxVolume = 1f;

        private RockSpawner _owner;
        private bool _alive = true;

        public bool IsAlive => _alive;

        public void Bind(RockSpawner owner)
        {
            _owner = owner;
            _alive = true;
            gameObject.SetActive(true);
        }

        public void Mine()
        {
            if (!_alive) return;
            _alive = false;
            SfxPlayer.PlayOneShot(breakSfx, transform.position, breakSfxVolume);
            OnMined?.Invoke(this);
            _owner?.NotifyMined(this);
        }
    }
}
