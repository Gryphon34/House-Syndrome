using UnityEngine;

namespace DayNightSystem
{
    public class Clock : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private DayNightManager dayNightManager;

        [System.Serializable]
        private class ClockForDay
        {
            public Transform hourHand;
            public Transform minuteHand;
        }

        [Tooltip("1일차 = Element 0, 2일차 = Element 1, ... 7일차 = Element 6")]
        [SerializeField] private ClockForDay[] clocksByDay = new ClockForDay[7];

        [Header("Rotation")]
        [SerializeField] private bool clockwise = true;
        [SerializeField, Range(0f, 24f)] private float hourOffset;

        [Header("Time Source")]
        [SerializeField] private bool useDayNightManagerTime = true;
        [SerializeField, Range(0f, 1440f)] private float localTimeMinutes;
        [SerializeField] private float localTimeScale = 60f;

        [Header("Day Start")]
        [SerializeField, Range(0f, 24f)] private float startHour = 8f;

        private const float MinutesPerDay = 1440f;

        private int GetCurrentDay()
        {
            int day = 1;
            if (SpawnManager.Instance != null)
            {
                day = Mathf.Clamp(SpawnManager.Instance.GetCurrentDay(), 1, clocksByDay.Length);
            }

            return day;
        }

        private ClockForDay GetCurrentClockForDay()
        {
            if (clocksByDay == null || clocksByDay.Length == 0) return null;

            int day = GetCurrentDay();

            int index = Mathf.Clamp(day - 1, 0, clocksByDay.Length - 1);
            return clocksByDay[index];
        }

        private void Awake()
        {
            if (useDayNightManagerTime && dayNightManager != null)
            {
                localTimeMinutes = dayNightManager.CurrentTime;
            }
            else
            {
                ResetClockForNewDay();
            }
        }

        private void Update()
        {
            var currentClock = GetCurrentClockForDay();
            if (currentClock == null || currentClock.hourHand == null || currentClock.minuteHand == null)
            {
                return;
            }

            float totalMinutes;

            if (useDayNightManagerTime)
            {
                totalMinutes = dayNightManager.CurrentTime;
            }
            else
            {
                localTimeMinutes = Mathf.Repeat(localTimeMinutes + Time.deltaTime * localTimeScale, MinutesPerDay);
                totalMinutes = localTimeMinutes;
            }

            float hours24 = totalMinutes / 60f;
            float minutes = totalMinutes % 60f;

            // Day 1: 시계 방향, Day 2~7: 시계 반대 방향
            int day = GetCurrentDay();
            float baseDir = clockwise ? 1f : -1f;
            float dayDir = day == 1 ? 1f : -1f;
            float dir = baseDir * dayDir;
            float hours12 = (hours24 + hourOffset) % 12f;

            float hourRotation = (hours12 / 12f) * 360f;
            currentClock.hourHand.localRotation = Quaternion.Euler(0f, 0f, dir * hourRotation);

            float minuteRotation = (minutes / 60f) * 360f;
            currentClock.minuteHand.localRotation = Quaternion.Euler(0f, 0f, dir * minuteRotation);
        }

        public void ResetClockForNewDay()
        {
            if (!useDayNightManagerTime)
            {
                float clampedHour = Mathf.Repeat(startHour, 24f);
                localTimeMinutes = clampedHour * 60f;
            }
        }
    }
}