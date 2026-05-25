using Conn.Core.World;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class FieldMonsterActorController : MonoBehaviour
    {
        [SerializeField] private string stateKey = string.Empty;
        [SerializeField] private Vector3 anchorPosition;
        [SerializeField] private Vector3 patrolTarget;
        [SerializeField] private Transform playerTarget;

        public string StateKey => stateKey;
        public Vector3 AnchorPosition => anchorPosition;
        public Vector3 PatrolTarget => patrolTarget;
        public bool PlayerDetected { get; private set; }

        public void Configure(string stateKey, Vector3 anchorPosition)
        {
            this.stateKey = stateKey ?? string.Empty;
            this.anchorPosition = anchorPosition;
            patrolTarget = anchorPosition;
        }

        public void SetPlayerTarget(Transform target)
        {
            playerTarget = target;
        }

        private void Start()
        {
            Tick(0f);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (string.IsNullOrWhiteSpace(stateKey) || GameSession.Instance == null)
            {
                return;
            }

            Tick(GameSession.Instance.State.World.FindFieldMonster(stateKey), deltaTime);
        }

        public void Tick(FieldMonsterState state, float deltaTime)
        {
            ApplyState(state, Mathf.Max(0f, deltaTime));
        }

        private void ApplyState(FieldMonsterState state, float deltaTime)
        {
            if (state == null || state.Defeated)
            {
                return;
            }

            if (state.Status == FieldMonsterStatus.Idle)
            {
                transform.position = anchorPosition;
                PlayerDetected = DetectPlayer(state);
                if (PlayerDetected)
                {
                    state.Status = FieldMonsterStatus.Chase;
                }
            }
            else if (state.Status == FieldMonsterStatus.Patrol)
            {
                PlayerDetected = DetectPlayer(state);
                if (PlayerDetected)
                {
                    state.Status = FieldMonsterStatus.Chase;
                    return;
                }

                ApplyPatrol(state, deltaTime);
            }
            else if (state.Status == FieldMonsterStatus.Chase)
            {
                ApplyChase(state, deltaTime);
            }
            else if (state.Status == FieldMonsterStatus.ReturnToAnchor)
            {
                ApplyReturnToAnchor(state, deltaTime);
            }
        }

        public bool DetectPlayer(FieldMonsterState state)
        {
            if (state == null || playerTarget == null)
            {
                return false;
            }

            var radius = Mathf.Max(0f, state.DetectionRadius);
            if (radius <= 0f)
            {
                return false;
            }

            return Vector3.Distance(transform.position, playerTarget.position) <= radius;
        }

        private void ApplyPatrol(FieldMonsterState state, float deltaTime)
        {
            if (patrolTarget == anchorPosition)
            {
                patrolTarget = BuildPatrolTarget(state);
            }

            var speed = Mathf.Max(0f, state.MoveSpeed);
            if (speed <= 0f)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, patrolTarget, speed * deltaTime);
            if (Vector3.Distance(transform.position, patrolTarget) <= 0.05f)
            {
                patrolTarget = patrolTarget == anchorPosition ? BuildPatrolTarget(state) : anchorPosition;
            }
        }

        private void ApplyChase(FieldMonsterState state, float deltaTime)
        {
            PlayerDetected = DetectPlayer(state);
            if (!PlayerDetected)
            {
                state.Status = FieldMonsterStatus.ReturnToAnchor;
                return;
            }

            if (playerTarget == null)
            {
                return;
            }

            var speed = Mathf.Max(0f, state.MoveSpeed);
            if (speed <= 0f)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, speed * deltaTime);
        }

        private void ApplyReturnToAnchor(FieldMonsterState state, float deltaTime)
        {
            var speed = Mathf.Max(0f, state.MoveSpeed);
            if (speed <= 0f)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, anchorPosition, speed * deltaTime);
            if (Vector3.Distance(transform.position, anchorPosition) <= 0.05f)
            {
                transform.position = anchorPosition;
                patrolTarget = anchorPosition;
                state.Status = FieldMonsterStatus.Patrol;
            }
        }

        private Vector3 BuildPatrolTarget(FieldMonsterState state)
        {
            var radius = Mathf.Max(0f, state.PatrolRadius);
            if (radius <= 0f)
            {
                return anchorPosition;
            }

            var direction = StableDirection(state.StateKey);
            return anchorPosition + new Vector3(direction.x * radius, 0f, direction.y * radius);
        }

        private static Vector2 StableDirection(string value)
        {
            var hash = 17;
            for (var i = 0; i < value.Length; i++)
            {
                hash = hash * 31 + value[i];
            }

            var angle = (Mathf.Abs(hash) % 360) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
