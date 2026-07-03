using System;

namespace CampLantern.Core
{
    /// <summary>
    /// 코인 지갑. 모든 코인 증감은 반드시 이 클래스를 통한다 — 외부에서 잔액 직접 세팅 불가.
    /// 소프트 캡 등 상위 경제 규칙은 P1 이후 (domain/economy.md).
    /// </summary>
    public class Wallet
    {
        public int Coins { get; private set; }

        /// <summary>잔액 변경 시 발화. 구독자는 OnDestroy/OnDisable에서 반드시 해제할 것 (rules/scripts.md).</summary>
        public event Action<int> CoinsChanged;

        public void Add(int amount)
        {
            if (amount <= 0) return;

            Coins += amount;
            CoinsChanged?.Invoke(Coins);
        }

        /// <summary>잔액이 충분하면 차감 후 true. 부족하면 잔액을 건드리지 않고 false.</summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0) return false;
            if (Coins < amount) return false;

            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return true;
        }
    }
}
