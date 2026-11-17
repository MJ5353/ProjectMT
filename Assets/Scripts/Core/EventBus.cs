using System;
using System.Collections.Generic;
using UnityEngine;

namespace MushTaker.Core
{
    /// <summary>
    /// 타입별 전역 이벤트 버스. 시스템끼리 직접 참조하지 않고 이벤트 구조체로만 소식을 주고받는다.
    /// 구독한 쪽은 OnDisable/OnDestroy에서 반드시 Unsubscribe 한다.
    /// </summary>
    public static class EventBus
    {
        static readonly List<Action> s_Clearers = new();

        static class Channel<T> where T : struct
        {
            public static Action<T> Handlers;
            static Channel() => s_Clearers.Add(() => Handlers = null);
        }

        // ==================================================

        public static void Subscribe<T>(Action<T> handler) where T : struct => Channel<T>.Handlers += handler;
        public static void Unsubscribe<T>(Action<T> handler) where T : struct => Channel<T>.Handlers -= handler;
        public static void Publish<T>(T evt) where T : struct => Channel<T>.Handlers?.Invoke(evt);

        // Enter Play Mode에서 도메인 리로드를 꺼도 지난 플레이의 구독이 남지 않게 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearAll()
        {
            foreach (var clear in s_Clearers)
            {
                clear();
            }
        }
    }
}