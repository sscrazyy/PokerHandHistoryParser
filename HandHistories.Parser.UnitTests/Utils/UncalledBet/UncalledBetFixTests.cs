﻿using HandHistories.Objects.Actions;
using HandHistories.Objects.Hand;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HandHistories.Parser.Utils.Uncalled;
using HandHistories.Objects.Cards;

namespace HandHistories.Parser.UnitTests.Utils.Uncalled
{
    [TestFixture]
    class UncalledBetFixTests
    {
        void TestUncalledbet(string expectedPlayer, decimal expectedAmount, HandHistory hand)
        {
            var actions = UncalledBet.Fix(hand.HandActions);

            var lastAction = actions.Last();

            Assert.AreEqual(expectedPlayer, lastAction.PlayerName);
            Assert.AreEqual(expectedAmount, lastAction.Amount);
        }

        [TestCase]
        public void UncalledBetTest_NoFixRequired_1()
        {
            HandHistory hand = new HandHistory();

            hand.HandActions = new List<HandAction>
            {
                new HandAction("P1", HandActionType.SMALL_BLIND, 0.1m, Street.Preflop),
                new HandAction("P2", HandActionType.BIG_BLIND, 0.2m, Street.Preflop),
                new HandAction("P1", HandActionType.CALL, 0.1m, Street.Preflop),
                new HandAction("P2", HandActionType.CHECK, 0m, Street.Preflop),

                new HandAction("P1", HandActionType.CHECK, 0m, Street.Flop),
                new HandAction("P2", HandActionType.BET, 0.1m, Street.Flop),
                new HandAction("P1", HandActionType.FOLD, 0m, Street.Flop),
                new HandAction("P2", HandActionType.UNCALLED_BET, 0.1m, Street.Flop),
            };

            TestUncalledbet("P2", 0.1m, hand);
        }

        [TestCase]
        public void UncalledBetTest_UncalledBet_1()
        {
            HandHistory hand = new HandHistory();

            hand.HandActions = new List<HandAction>
            {
                new HandAction("P1", HandActionType.SMALL_BLIND, 0.1m, Street.Preflop),
                new HandAction("P2", HandActionType.BIG_BLIND, 0.2m, Street.Preflop),
                new HandAction("P1", HandActionType.CALL, 0.1m, Street.Preflop),
                new HandAction("P2", HandActionType.CHECK, 0m, Street.Preflop),

                new HandAction("P1", HandActionType.CHECK, 0m, Street.Flop),
                new HandAction("P2", HandActionType.BET, 0.1m, Street.Flop),
                new HandAction("P1", HandActionType.FOLD, 0m, Street.Flop),
            };

            TestUncalledbet("P2", 0.1m, hand);
        }

        [TestCase]
        public void UncalledBetTest_UncalledBB_1()
        {
            HandHistory hand = new HandHistory();

            hand.HandActions = new List<HandAction>
            {
                new HandAction("SB", HandActionType.SMALL_BLIND, 1m, Street.Preflop),
                new HandAction("BB", HandActionType.BIG_BLIND, 2m, Street.Preflop),
            };

            TestUncalledbet("BB", 1m, hand);
        }

        [TestCase]
        public void UncalledBetTest_UncalledRaise_1()
        {
            HandHistory hand = new HandHistory();

            hand.HandActions = new List<HandAction>
            {
                new HandAction("P1", HandActionType.SMALL_BLIND, 0.1m, Street.Preflop),
                new HandAction("P2", HandActionType.BIG_BLIND, 0.2m, Street.Preflop),
                new HandAction("P1", HandActionType.CALL, 0.1m, Street.Preflop),
                new HandAction("P2", HandActionType.CHECK, 0m, Street.Preflop),

                new HandAction("P1", HandActionType.CHECK, 0m, Street.Flop),
                new HandAction("P2", HandActionType.BET, 0.1m, Street.Flop),
                new HandAction("P1", HandActionType.RAISE, 0.6m, Street.Flop),
                new HandAction("P2", HandActionType.FOLD, Street.Flop),
            };

            TestUncalledbet("P1", 0.5m, hand);
        }

        [TestCase]
        public void UncalledBetTest_PartiallyUncalledRaise_1()
        {
            HandHistory hand = new HandHistory();

            hand.HandActions = new List<HandAction>
            {
                new HandAction("P1", HandActionType.SMALL_BLIND, 1m, Street.Preflop),
                new HandAction("P2", HandActionType.BIG_BLIND, 2m, Street.Preflop),
                new HandAction("P1", HandActionType.CALL, 1m, Street.Preflop),
                new HandAction("P2", HandActionType.CHECK, 0m, Street.Preflop),

                new HandAction("P1", HandActionType.CHECK, 0m, Street.Flop),
                new HandAction("P2", HandActionType.BET, 1m, Street.Flop),
                new HandAction("P1", HandActionType.RAISE, 6m, Street.Flop),
                new HandAction("P2", HandActionType.CALL, 1m, Street.Flop, true),
            };

            TestUncalledbet("P1", 4m, hand);
        }

        [TestCase]
        public void UncalledBetTest_PartiallyUncalledRaise_2()
        {
            HandHistory hand = new HandHistory();

            hand.HandActions = new List<HandAction>
            {
                new HandAction("P1", HandActionType.SMALL_BLIND, 1m, Street.Preflop),
                new HandAction("P2", HandActionType.BIG_BLIND, 2m, Street.Preflop),
                new HandAction("P3", HandActionType.CALL, 2m, Street.Preflop),
                new HandAction("P1", HandActionType.CALL, 1m, Street.Preflop),
                new HandAction("P2", HandActionType.CHECK, 0m, Street.Preflop),

                new HandAction("P1", HandActionType.CHECK, 0m, Street.Flop),
                new HandAction("P2", HandActionType.BET, 1m, Street.Flop),
                new HandAction("P3", HandActionType.RAISE, 6m, Street.Flop),
                new HandAction("P1", HandActionType.CALL, 1m, Street.Flop, true),
                new HandAction("P2", HandActionType.CALL, 3m, Street.Flop, true),
            };

            TestUncalledbet("P3", 2m, hand);
        }

        [TestCase]
        public void UncalledBetTest_PartiallyUncalledRaise_3()
        {
            HandHistory hand = new HandHistory();

            hand.HandActions = new List<HandAction>
            {
                new HandAction("P1", HandActionType.SMALL_BLIND, 1m, Street.Preflop),
                new HandAction("P2", HandActionType.BIG_BLIND, 2m, Street.Preflop),
                new HandAction("P3", HandActionType.RAISE, 4m, Street.Preflop),
                new HandAction("P4", HandActionType.CALL, 2m, Street.Preflop, true),
            };

            TestUncalledbet("P3", 2m, hand);
        }

        [TestCase]
        public void UncalledBetTest_PartiallyUncalledBB_1()
        {
            HandHistory hand = new HandHistory();

            hand.HandActions = new List<HandAction>
            {
                new HandAction("P1", HandActionType.SMALL_BLIND, 1m, Street.Preflop),
                new HandAction("P2", HandActionType.BIG_BLIND, 6m, Street.Preflop),
                new HandAction("P3", HandActionType.CALL, 3m, Street.Preflop, true),
                new HandAction("P4", HandActionType.CALL, 1m, Street.Preflop, true),
            };

            TestUncalledbet("P2", 3m, hand);
        }
    }
}
