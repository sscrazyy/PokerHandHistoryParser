﻿using System.Collections.Generic;
using HandHistories.Objects.Actions;
using HandHistories.Objects.Cards;
using NUnit.Framework;

namespace HandHistories.Parser.UnitTests.Parsers.HandParserTests.HandActionTests
{
    [TestFixture]
    class HandParserHandActionTestsWinningPokerImpl : HandParserHandActionTests
    {
        public HandParserHandActionTestsWinningPokerImpl()
            : base("WinningPoker")
        {
        }

        [Test]
        public void StrangePlayerNames_Works()
        {
            List<HandAction> expectedActions = new List<HandAction>()
                                    {
                                        new HandAction("do not-call", HandActionType.SMALL_BLIND, 2m, Street.Preflop),
                                        new HandAction("OhGoodGolly", HandActionType.BIG_BLIND, 4m, Street.Preflop),

                                        new HandAction("dont.do.it", HandActionType.FOLD, 0, Street.Preflop),
                                        new HandAction("COMON-JOE-JUG", HandActionType.RAISE, 14, Street.Preflop),
                                        new HandAction("do not-call", HandActionType.CALL, 12m, Street.Preflop),
                                        new HandAction("OhGoodGolly", HandActionType.CALL, 10, Street.Preflop),

                                        new HandAction("do not-call", HandActionType.CHECK, 0, Street.Flop),
                                        new HandAction("OhGoodGolly", HandActionType.CHECK, 0, Street.Flop),
                                        new HandAction("COMON-JOE-JUG", HandActionType.BET, 39.75m, Street.Flop),
                                        new HandAction("do not-call", HandActionType.FOLD, 0, Street.Flop),
                                        new HandAction("OhGoodGolly", HandActionType.FOLD, 0, Street.Flop),

                                        new HandAction("COMON-JOE-JUG", HandActionType.UNCALLED_BET, 39.75m, Street.Flop),                  
                                        new WinningsAction("COMON-JOE-JUG", HandActionType.WINS, 25.75m, 0),
                                    };

            TestParseActions("StrangePlayerNames", expectedActions);
        }

        [Test]
        public void UncalledBet_Works()
        {
            List<HandAction> expectedActions = new List<HandAction>()
                           {
                               new HandAction("Shipologist", HandActionType.SMALL_BLIND, 0.10m, Street.Preflop),
                               new HandAction("borjilius79", HandActionType.BIG_BLIND, 0.25m, Street.Preflop),

                               new HandAction("LadyStack", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("FetusMunch", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("xXxAudiA5xXx", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("Daviciko", HandActionType.RAISE, 0.75m, Street.Preflop),
                               new HandAction("Shipologist", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("borjilius79", HandActionType.FOLD, 0, Street.Preflop),

                               new HandAction("Daviciko", HandActionType.UNCALLED_BET, 0.50m, Street.Preflop),
                               new WinningsAction("Daviciko", HandActionType.WINS, 0.35m,0),
                           };

            TestParseActions("UncalledBet", expectedActions);
        }

        //[Test]
        //public void AllInHand_NeedsRaiseAdjusting_Works()
        //{
        //    List<HandAction> expectedActions = new List<HandAction>()
        //                            {
        //                                new HandAction("BiatchPeople", HandActionType.SMALL_BLIND, 150m, Street.Preflop),
        //                                new HandAction("Bluf_To_Much", HandActionType.BIG_BLIND, 300m, Street.Preflop),
                                        
        //                                new HandAction("Ravenswood13", HandActionType.FOLD, 0m, Street.Preflop),                                     
        //                                new HandAction("Crazy Elior", HandActionType.RAISE, 600m, Street.Preflop),
        //                                new HandAction("joiso", HandActionType.RAISE, 900m, Street.Preflop),
        //                                new HandAction("Ilari FIN", HandActionType.FOLD, 0m, Street.Preflop),
        //                                new HandAction("LewisFriend", HandActionType.CALL, 900m, Street.Preflop),
        //                                new HandAction("BiatchPeople", HandActionType.FOLD, 0m, Street.Preflop),
        //                                new HandAction("Bluf_To_Much", HandActionType.CALL, 600m, Street.Preflop),
        //                                new HandAction("Crazy Elior", HandActionType.CALL, 300m, Street.Preflop),

        //                                new HandAction("Bluf_To_Much", HandActionType.CHECK, 0m, Street.Flop),
        //                                new HandAction("Crazy Elior", HandActionType.CHECK, 0m, Street.Flop),
        //                                new HandAction("joiso", HandActionType.BET, 300m, Street.Flop),
        //                                new HandAction("LewisFriend", HandActionType.CALL, 300m, Street.Flop),
        //                                new HandAction("Bluf_To_Much", HandActionType.CALL, 300m, Street.Flop),
        //                                new AllInAction("Crazy Elior", 382.50m, Street.Flop, true),
        //                                new HandAction("joiso", HandActionType.CALL, 82.50m, Street.Flop),
        //                                new HandAction("LewisFriend", HandActionType.CALL, 82.50m, Street.Flop),
        //                                new HandAction("Bluf_To_Much", HandActionType.CALL, 82.50m, Street.Flop),
                                        
        //                                new HandAction("Bluf_To_Much", HandActionType.CHECK, 0m, Street.Turn),
        //                                new HandAction("joiso", HandActionType.BET, 600m, Street.Turn),
        //                                new HandAction("LewisFriend", HandActionType.CALL, 600m, Street.Turn),
        //                                new HandAction("Bluf_To_Much", HandActionType.CALL, 600m, Street.Turn),

        //                                new HandAction("Bluf_To_Much", HandActionType.BET, 600m, Street.River),
        //                                new HandAction("joiso", HandActionType.FOLD, 0, Street.River),
        //                                new HandAction("LewisFriend", HandActionType.RAISE, 1200m, Street.River),
        //                                new HandAction("Bluf_To_Much", HandActionType.CALL, 600m, Street.River),

        //                                new HandAction("LewisFriend", HandActionType.SHOW, 0, Street.Showdown),
        //                                new HandAction("Bluf_To_Much", HandActionType.SHOW, 0, Street.Showdown),
        //                                new WinningsAction("Bluf_To_Much", HandActionType.WINS_SIDE_POT, 2100, 1),
        //                                new WinningsAction("LewisFriend", HandActionType.WINS_SIDE_POT, 2100, 1),
                                        
        //                                new HandAction("Crazy Elior", HandActionType.SHOW, 0, Street.Showdown),
        //                                new WinningsAction("Crazy Elior", HandActionType.WINS, 2637.50m, 0),
        //                                new WinningsAction("LewisFriend", HandActionType.WINS, 2637.50m, 0),

                                        
        //                            };

        //    TestParseActions("NeedsRaiseAdjusting", expectedActions);
        //}

        //[Test]
        //public void AllInHand_NeedsRaiseAdjusting_BigBlindOptionRaisesOption_Works()
        //{
        //    List<HandAction> expectedActions = new List<HandAction>()
        //                            {
        //                                new HandAction("idirabotai", HandActionType.SMALL_BLIND, 0.5m, Street.Preflop),
        //                                new HandAction("jweeke", HandActionType.BIG_BLIND, 1m, Street.Preflop),
        //                                new HandAction("idirabotai", HandActionType.CALL, 0.5m, Street.Preflop),
        //                                new HandAction("jweeke", HandActionType.RAISE, 2m, Street.Preflop),
        //                                new HandAction("idirabotai", HandActionType.CALL, 2m, Street.Preflop),

        //                                new HandAction("jweeke", HandActionType.BET, 4m, Street.Flop),
        //                                new HandAction("idirabotai", HandActionType.CALL, 4m, Street.Flop),

        //                                new HandAction("jweeke", HandActionType.CHECK, 0, Street.Turn),
        //                                new HandAction("idirabotai", HandActionType.BET, 7m, Street.Turn),
        //                                new HandAction("jweeke", HandActionType.FOLD, 0, Street.Turn),

        //                                new HandAction("idirabotai", HandActionType.UNCALLED_BET, 7m, Street.Turn),
        //                                new WinningsAction("idirabotai", HandActionType.WINS, 13.50m, 0),
        //                                new HandAction("idirabotai", HandActionType.MUCKS, 0, Street.Showdown)
        //                            };

        //    TestParseActions("BigBlindOptionRaisesOption", expectedActions);
        //}

        protected override List<HandAction> ExpectedHandActionsBasicHand
        {
            get
            {
                return new List<HandAction>()
                           {
                               new HandAction("HanSoloDolo", HandActionType.SMALL_BLIND, 2m, Street.Preflop),
                               new HandAction("ap1104", HandActionType.BIG_BLIND, 4m, Street.Preflop),

                               new HandAction("Pokergodacolyte", HandActionType.RAISE, 12, Street.Preflop),
                               new HandAction("cuzimwhite", HandActionType.CALL, 12, Street.Preflop),
                               new HandAction("primume", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("HanSoloDolo", HandActionType.CALL, 10m, Street.Preflop),
                               new HandAction("ap1104", HandActionType.CALL, 8m, Street.Preflop),

                               new HandAction("HanSoloDolo", HandActionType.CHECK, 0m, Street.Flop),
                               new HandAction("ap1104", HandActionType.CHECK, 0m, Street.Flop),
                               new HandAction("Pokergodacolyte", HandActionType.CHECK, 0m, Street.Flop),
                               new HandAction("cuzimwhite", HandActionType.CHECK, 0m, Street.Flop),

                               new HandAction("HanSoloDolo", HandActionType.CHECK, 0m, Street.Turn),
                               new HandAction("ap1104", HandActionType.CHECK, 0m, Street.Turn),
                               new HandAction("Pokergodacolyte", HandActionType.CHECK, 0m, Street.Turn),
                               new HandAction("cuzimwhite", HandActionType.CHECK, 0m, Street.Turn),

                               new HandAction("HanSoloDolo", HandActionType.CHECK, 0m, Street.River),
                               new HandAction("ap1104", HandActionType.CHECK, 0m, Street.River),
                               new HandAction("Pokergodacolyte", HandActionType.CHECK, 0m, Street.River),
                               new HandAction("cuzimwhite", HandActionType.CHECK, 0m, Street.River),

                               new WinningsAction("HanSoloDolo", HandActionType.WINS, 33.75m,0),
                           };
            }
        }

        protected override List<HandAction> ExpectedHandActionsFoldedPreflop
        {
            get
            {
                return new List<HandAction>()
                           {
                               new HandAction("LadyStack", HandActionType.SMALL_BLIND, 0.1m, Street.Preflop),
                               new HandAction("FetusMunch", HandActionType.BIG_BLIND, 0.25m, Street.Preflop),

                               new HandAction("xXxAudiA5xXx", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("johna52801", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("Shipologist", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("borjilius79", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("LadyStack", HandActionType.RAISE, 0.70m, Street.Preflop),
                               new HandAction("FetusMunch", HandActionType.FOLD, 0, Street.Preflop),

                               new HandAction("LadyStack", HandActionType.UNCALLED_BET, 0.55m, Street.Preflop),
                               new WinningsAction("LadyStack", HandActionType.WINS, 0.25m, 0),
                           };
            }
        }

        protected override List<HandAction> ExpectedHandActions3BetHand
        {
            get
            {
                return new List<HandAction>()
                           {
                               new HandAction("digbick30", HandActionType.SMALL_BLIND, 2m, Street.Preflop),
                               new HandAction("STOPCRYINGB79", HandActionType.BIG_BLIND, 4m, Street.Preflop),

                               new HandAction("digbick30", HandActionType.RAISE, 6, Street.Preflop),
                               new HandAction("STOPCRYINGB79", HandActionType.RAISE, 20, Street.Preflop),
                               new HandAction("digbick30", HandActionType.CALL, 16m, Street.Preflop),

                               new HandAction("STOPCRYINGB79", HandActionType.CHECK, 0, Street.Flop),
                               new HandAction("digbick30", HandActionType.CHECK, 0, Street.Flop),

                               new HandAction("STOPCRYINGB79", HandActionType.CHECK, 0, Street.Turn),
                               new HandAction("digbick30", HandActionType.CHECK, 0, Street.Turn),

                               new HandAction("STOPCRYINGB79", HandActionType.CHECK, 0, Street.River),
                               new HandAction("digbick30", HandActionType.BET, 15.66m, Street.River),
                               new HandAction("STOPCRYINGB79", HandActionType.FOLD, 0, Street.River),

                               new HandAction("digbick30", HandActionType.UNCALLED_BET, 15.66m, Street.River),
                               new WinningsAction("digbick30", HandActionType.WINS, 23m, 0),
                           };
            }
        }

        protected override List<HandAction> ExpectedHandActionsAllInHand
        {
            get
            {
                return new List<HandAction>()
                           {
                               new HandAction("do not-call", HandActionType.SMALL_BLIND, 2m, Street.Preflop),
                               new HandAction("digbick30", HandActionType.BIG_BLIND, 4m, Street.Preflop),

                               new HandAction("do not-call", HandActionType.RAISE, 10, Street.Preflop),
                               new HandAction("digbick30", HandActionType.RAISE, 32, Street.Preflop),
                               new HandAction("do not-call", HandActionType.CALL, 24m, Street.Preflop),

                               new HandAction("digbick30", HandActionType.BET, 71m, Street.Flop),
                               new AllInAction("do not-call", 124m, Street.Flop, true),
                               new HandAction("digbick30", HandActionType.CALL, 53m, Street.Flop),

                               new WinningsAction("do not-call", HandActionType.WINS, 159m, 0)
                           };
            }
        }

        protected override List<HandAction> ExpectedOmahaHiLoHand
        {
            get
            {
                return new List<HandAction>()
                           {
                               new HandAction("ColFortune81", HandActionType.SMALL_BLIND, 0.5m, Street.Preflop),
                               new HandAction("Belkiss2", HandActionType.BIG_BLIND, 1m, Street.Preflop),
                               new HandAction("ribby53", HandActionType.BIG_BLIND, 1m, Street.Preflop),

                               new HandAction("funkyOne81", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("ribby53", HandActionType.CHECK, 0, Street.Preflop),
                               new HandAction("Zident", HandActionType.FOLD, 0, Street.Preflop),
                               new HandAction("mq260", HandActionType.CALL, 1m, Street.Preflop),
                               new HandAction("ColFortune81", HandActionType.FOLD, 0m, Street.Preflop),
                               new HandAction("Belkiss2", HandActionType.CHECK, 0m, Street.Preflop),
                               
                               new HandAction("Belkiss2", HandActionType.CHECK, 0m, Street.Flop),
                               new HandAction("ribby53", HandActionType.CHECK, 0m, Street.Flop),
                               new HandAction("mq260", HandActionType.CHECK, 0m, Street.Flop),

                               new HandAction("Belkiss2", HandActionType.CHECK, 0m, Street.Turn),
                               new HandAction("ribby53", HandActionType.CHECK, 0m, Street.Turn),
                               new HandAction("mq260", HandActionType.CHECK, 0m, Street.Turn),

                               new HandAction("Belkiss2", HandActionType.CHECK, 0m, Street.River),
                               new HandAction("ribby53", HandActionType.CHECK, 0m, Street.River),
                               new HandAction("mq260", HandActionType.CHECK, 0m, Street.River),

                               new HandAction("Belkiss2", HandActionType.SHOW, 0m, Street.Showdown),
                               new HandAction("ribby53", HandActionType.MUCKS,0m, Street.Showdown),
                               new HandAction("mq260", HandActionType.MUCKS,0m, Street.Showdown),

                               new WinningsAction("Belkiss2", HandActionType.WINS, 1.67m, 0),
                               new WinningsAction("Belkiss2", HandActionType.WINS, 1.67m, 0)
                           };
            }
        }
    }
}
