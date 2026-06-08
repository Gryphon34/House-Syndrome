using UnityEngine;

namespace DayNightSystem
{
    /// <summary>
    /// Represents a clock that visually displays the current time based on the day-night cycle.
    /// Updates the rotation of the hour and minute hands dynamically.
    ///
    /// [추가] visualSpeedMultiplier: 시계 귀신이 실패 시 이 값을 올려
    ///        시계 바늘을 실제 시간과 무관하게 빠르게 돌려 조급함을 유발합니다.
    /// </summary>
    public class Clock : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Reference to the DayNightManager that provides the current time.")]
        [SerializeField] private DayNightManager dayNightManager;
        [Tooltip("Transform representing the hour hand of the clock.")]
        [SerializeField] private Transform hourHand;
        [Tooltip("Transform representing the minute hand of the clock.")]
        [SerializeField] private Transform minuteHand;

        [Header("Rotation")]
        [Tooltip("Determines whether the clock hands rotate clockwise or counterclockwise.")]
        [SerializeField] private bool clockwise = true;
        [Tooltip("Offset applied to the hour hand rotation, in hours.")]
        [SerializeField, Range(0f, 24f)] private float hourOffset;

        [Header("Ghost Control")]
        [Tooltip("시계 귀신이 실패할 때마다 이 값을 증가시켜 바늘을 빠르게 돌립니다.\n" +
                 "1 = 정상 속도 / 2 = 2배 빠름 / 최대 GhostManager에서 제어.")]
        public float visualSpeedMultiplier = 1f;

        // 가상의 추가 경과 분 (실제 게임 시간과 무관하게 바늘에만 적용)
        private float _bonusMinutes = 0f;

        private void Update()
        {
            // multiplier > 1 일 때 매 프레임 추가 분(分) 누적
            // (1분 = 60초이므로 ÷60 → 1초에 1/60분씩 실시간 시간이 흐름 ≒ 정상 속도 기준)
            _bonusMinutes += Time.deltaTime * (visualSpeedMultiplier - 1f) / 60f * 60f;
            // 정리: _bonusMinutes += Time.deltaTime * (visualSpeedMultiplier - 1f)
            // multiplier=1 → +0 / multiplier=2 → 실제 시간만큼 추가로 쌓임 → 2배 속도

            float totalMinutes = dayNightManager.CurrentTime + _bonusMinutes;
            float hours24 = totalMinutes / 60f;
            float minutes  = totalMinutes % 60f;
            float dir      = clockwise ? 1f : -1f;
            float hours12  = (hours24 + hourOffset) % 12f;

            float hourRotation   = (hours12 / 12f) * 360f;
            float minuteRotation = (minutes  / 60f) * 360f;

            hourHand.localRotation   = Quaternion.Euler(0f, 0f, dir * hourRotation);
            minuteHand.localRotation = Quaternion.Euler(0f, 0f, dir * minuteRotation);
        }

        /// <summary>
        /// 귀신 제거·플레이어 탈출 시 호출해 시계를 정상 속도로 복원합니다.
        /// </summary>
        public void ResetVisualSpeed()
        {
            visualSpeedMultiplier = 1f;
            _bonusMinutes = 0f;
        }
    }
}
