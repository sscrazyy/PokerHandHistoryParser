﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using HandHistories.Objects.Actions;
using HandHistories.Objects.Cards;
using HandHistories.Objects.GameDescription;
using HandHistories.Objects.Players;
using HandHistories.Parser.Parsers.Exceptions;
using HandHistories.Parser.Parsers.FastParser.Base;

namespace HandHistories.Parser.Parsers.FastParser.IPoker
{
    public sealed class IPokerFastParserImpl : HandHistoryParserFastImpl
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly NumberFormatInfo NumberFormatInfo_EURO = new NumberFormatInfo
        {
            NegativeSign = "-",
            CurrencyDecimalSeparator = ".",
            CurrencyGroupSeparator = ",",
            CurrencySymbol = "€"
        };

        private static readonly NumberFormatInfo NumberFormatInfo_USD = new NumberFormatInfo
        {
            NegativeSign = "-",
            CurrencyDecimalSeparator = ".",
            CurrencyGroupSeparator = ",",
            CurrencySymbol = "€"
        };

        private static readonly NumberFormatInfo NumberFormatInfo_GBP = new NumberFormatInfo
        {
            NegativeSign = "-",
            CurrencyDecimalSeparator = ".",
            CurrencyGroupSeparator = ",",
            CurrencySymbol = "£"
        };

        private readonly bool _isIpoker2;
        private static int CurrencyParsingErrors;
        [ThreadStatic]
        private static NumberFormatInfo NumberFormatInfo = NumberFormatInfo_EURO;

        public IPokerFastParserImpl(bool isIpoker2 = false)
        {
            _isIpoker2 = isIpoker2;
        }

        public override SiteName SiteName
        {
            get { return (_isIpoker2) ? SiteName.IPoker2 : SiteName.IPoker; }
        }

        public override bool  RequiresAdjustedRaiseSizes
        {
	        get 
	        { 
		         return true;
	        }
        }

        public override bool RequiresActionSorting
        {
            get { return true; }
        }

        public override bool RequiresAllInDetection
        {
            get { return true; }
        }

        public override bool RequiresTotalPotCalculation
        {
            get { return true; }
        }

        public override bool RequiresUncalledBetFix
        {
            get { return true; }
        }

        protected override string[] SplitHandsLines(string handText)
        {
            return Utils.XMLHandLineSplitter.Split(handText);
        }

        /*
            <player seat="3" name="RodriDiaz3" chips="$2.25" dealer="0" win="$0" bet="$0.08" rebuy="0" addon="0" />
            <player seat="8" name="Kristi48ru" chips="$6.43" dealer="1" win="$0.23" bet="$0.16" rebuy="0" addon="0" />
        */

        int GetSeatNumberFromPlayerLine(string playerLine)
        {
            int seatOffset = playerLine.IndexOf(" s", StringComparison.Ordinal) + 7;
            int seatEndOffset = playerLine.IndexOf('"', seatOffset);
            string seatNumberString = playerLine.Substring(seatOffset, seatEndOffset - seatOffset);
            return Int32.Parse(seatNumberString);            
        }

        bool IsPlayerLineDealer(string playerLine)
        {
            int dealerOffset = playerLine.IndexOf(" d", StringComparison.Ordinal);
            int dealerValue = Int32.Parse(" " + playerLine[dealerOffset + 9]);
            return dealerValue == 1;
        }

        decimal GetStackFromPlayerLine(string playerLine)
        {
            int stackStartPos = playerLine.IndexOf(" c", StringComparison.Ordinal) + 8;
            int stackEndPos = playerLine.IndexOf('"', stackStartPos) - 1;
            string stackString = playerLine.Substring(stackStartPos, stackEndPos - stackStartPos + 1);
            return ParseDecimal(stackString);
        }

        decimal GetWinningsFromPlayerLine(string playerLine)
        {
            int stackStartPos = playerLine.IndexOf(" w", StringComparison.Ordinal) + 6;
            int stackEndPos = playerLine.IndexOf('"', stackStartPos) - 1;
            string stackString = playerLine.Substring(stackStartPos, stackEndPos - stackStartPos + 1);
            if (stackString == "")
            {
                return 0;
            }
            return ParseDecimal(stackString);
        }

        string GetNameFromPlayerLine(string playerLine)
        {
            int nameStartPos = playerLine.IndexOf(" n", StringComparison.Ordinal) + 7;
            int nameEndPos = playerLine.IndexOf('"', nameStartPos) - 1;
            string name = playerLine.Substring(nameStartPos, nameEndPos - nameStartPos + 1);
            return name;
        }


        protected override int ParseDealerPosition(string[] handLines)
        {

            string[] playerLines = GetPlayerLinesFromHandLines(handLines);

            for (int i = 0; i < playerLines.Count(); i++)
            {
                string playerLine = playerLines[i];
                if (IsPlayerLineDealer(playerLine))
                {
                    return GetSeatNumberFromPlayerLine(playerLine);                    
                }
            }

            throw new Exception("Could not locate dealer");
        }

        protected override DateTime ParseDateUtc(string[] handLines)
        {
            //<startdate>2012-05-28 16:52:05</startdate>
            string dateLine = GetStartDateFromHandLines(handLines);
            int startPos = dateLine.IndexOf('>') + 1;
            int endPos = dateLine.LastIndexOf('<') - 1;
            string dateString = dateLine.Substring(startPos, endPos - startPos + 1);
            DateTime dateTime = DateTime.Parse(dateString);
            //TimeZoneUtil.ConvertDateTimeToUtc(dateTime, TimeZoneType.GMT);

            return dateTime;
        }

        private static readonly Regex SessionGroupRegex = new Regex("<session.*?session>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline);
        private static readonly Regex GameGroupRegex = new Regex("<game.*?game>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline);
        public override IEnumerable<string> SplitUpMultipleHands(string rawHandHistories)
        {
            //Remove XML headers if necessary 
            rawHandHistories = rawHandHistories.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n", "");

            //Two Cases - Case 1, we have a single <session> tag holding multiple <game> tags
            //            Case 2, we have multiple <session> tags each holding a single <game> tag.
            //            We need our enumerable to have only case 2 representations for parsing

            if (rawHandHistories.IndexOf("<session", StringComparison.Ordinal) == rawHandHistories.LastIndexOf("<session", StringComparison.Ordinal))
            {
                //We are case 1 - convert to case 2

                int endOfGeneralInfoIndex = rawHandHistories.IndexOf("</general>", StringComparison.Ordinal);

                if (endOfGeneralInfoIndex == -1)
                {
                    // log the first 1000 chars of the file, so we can at least guess what's the problem
                    logger.Fatal("IPokerFastParserImpl.SplitUpMultipleHands(): Encountered a weird file\r\n{0}", rawHandHistories.Substring(0,1000));
                }

                string generalInfoString = rawHandHistories.Substring(0, endOfGeneralInfoIndex + 10);

                MatchCollection gameMatches = GameGroupRegex.Matches(rawHandHistories, endOfGeneralInfoIndex + 9);
                foreach (Match gameMatch in gameMatches)
                {
                    string fullGameString = generalInfoString + "\r\n" + gameMatch.Value + "\r\n</session>";
                    yield return fullGameString;
                }
            }
            else
            {
                //We are case 2
                MatchCollection matches = SessionGroupRegex.Matches(rawHandHistories);

                foreach (Match match in matches)
                {
                    yield return match.Value;
                }                
            }
        }
        
        protected override PokerFormat ParsePokerFormat(string[] handLines)
        {
            return PokerFormat.CashGame;
        }

        // For now (4/17/2012) only need Game # in Miner and using Regexes. Will convert to faster mechanism soon.
        private static readonly Regex HandIdRegex = new Regex("(?<=gamecode=\")\\d+", RegexOptions.Compiled);
        protected override long ParseHandId(string[] handLines)
        {
            foreach (var handLine in handLines)
            {
                var match = HandIdRegex.Match(handLine);
                if (match.Success)
                {
                    return long.Parse(match.Value.Replace("-", ""));
                }
            }

            throw new HandIdException(handLines[1], "Couldn't find handid");
        }

        protected override long ParseTournamentId(string[] handLines)
        {
            throw new NotImplementedException();
        }

        protected override string ParseTableName(string[] handLines)
        {
            //<tablename>Ambia, 98575671</tablename>
            string tableNameLine = GetTableNameLineFromHandLines(handLines);
            int tableNameStartIndex = tableNameLine.IndexOf('>') + 1;
            int tableNameEndIndex = tableNameLine.LastIndexOf('<') - 1;

            string tableName = tableNameLine.Substring(tableNameStartIndex, tableNameEndIndex - tableNameStartIndex + 1);

            return tableName;
        }

        protected override SeatType ParseSeatType(string[] handLines)
        {
            string[] playerLines = GetPlayerLinesFromHandLines(handLines);
            int numPlayers = playerLines.Count();

            if (numPlayers <= 2)
            {
                return SeatType.FromMaxPlayers(2);
            }
            if (numPlayers <= 6)
            {
                return SeatType.FromMaxPlayers(6);
            }
            if (numPlayers <= 9)
            {
                return SeatType.FromMaxPlayers(9);
            }

            return SeatType.FromMaxPlayers(10);   
        }

        protected string GetGameTypeLineFromHandLines(string[] handLines)
        {
            //This is the 4th line if we have the <session> tag header
            return handLines[3];
        }

        protected string GetTableNameLineFromHandLines(string[] handLines)
        {
            //This is the 5th line if we have the <session> tag header
            return handLines[4];
        }

        protected string GetStartDateFromHandLines(string[] handLines)
        {
            // in order to find the exact date of the hand we search the startdate of the hand ( and not the table )
            
            for(int i=0; i<= handLines.Count(); i++)
            {
                if(handLines[i].Contains("gamecode=\""))
                {
                    return handLines[i + 2];
                }
            }

            // if we're unable to find the dateline for the hand, we just use the date for the table
            return handLines[8];
        }

        protected string[] GetPlayerLinesFromHandLines(string[] handLines)
        {
            /*
              Returns all of the detail lines between the <players> tags
              <players> <-- Line offset 22
                <player seat="1" name="Leichenghui" chips="£1,866.23" dealer="1" win="£0" bet="£5" rebuy="0" addon="0" />
                ...
                <player seat="10" name="CraigyColesBRL" chips="£2,297.25" dealer="0" win="£15" bet="£10" rebuy="0" addon="0" />
              </players>             
             */


            int offset = GetFirstPlayerIndex(handLines);
            List<string> playerLines = new List<string>();

            string line = handLines[offset];
            line = line.TrimStart();
            while (offset < handLines.Count() && line[1] != '/')
            {
                playerLines.Add(line);
                offset++;

                if (offset >= handLines.Count())
                    break;

                line = handLines[offset];
                line = line.TrimStart();
            }

            return playerLines.ToArray();
        }

        private int GetFirstPlayerIndex(string[] handLines)
        {
            for (int i = 18; i < handLines.Length; i++)
            {
                if (handLines[i][1] == 'p')
                {
                    return i + 1;
                }
            }
            throw new IndexOutOfRangeException("Did not find first player");
        }

        protected string[] GetCardLinesFromHandLines(string[] handLines)
        {
            List<string> cardLines = new List<string>();
            for (int i = 0; i < handLines.Length; i++)
            {
                string handLine = handLines[i];
                handLine = handLine.TrimStart();

                //If we don't have these letters at these positions, we're not a hand line
                if (handLine[1] != 'c' || handLine[2] != 'a')
                {
                    continue;
                }

                cardLines.Add(handLine);
            }

            return cardLines.ToArray();
        }

        protected override GameType ParseGameType(string[] handLines)
        {
            /*
             * NLH <gametype>Holdem NL $2/$4</gametype>  
             * NLH <gametype>Holdem PL $2/$4</gametype>    
             * FL  <gametype>Holdem L $2/$4</gametype>
             * PLO <gametype>Omaha PL $0.50/$1</gametype>
             */

            string gameTypeLine = GetGameTypeLineFromHandLines(handLines);

            //If this is an H we're a Holdem, if O, Omaha
            char gameTypeChar = gameTypeLine[10]; 

            if (gameTypeChar == 'O')
            {
                return GameType.PotLimitOmaha;
            }

            //If this is an N, we're NLH, if L - FL
            char holdemTypeChar = gameTypeLine[17]; 

            if (holdemTypeChar == 'L')
            {
                return GameType.FixedLimitHoldem;
            }

            if (holdemTypeChar == 'N')
            {
                return GameType.NoLimitHoldem;
            }

            if (holdemTypeChar == 'P')
            {
                return GameType.PotLimitHoldem;
            }

            throw new Exception("Could not parse GameType for hand.");
        }

        protected override TableType ParseTableType(string[] handLines)
        {
            string tableName = ParseTableName(handLines);

            if (tableName.StartsWith("(Shallow)"))
            {
                return TableType.FromTableTypeDescriptions(TableTypeDescription.Shallow);
            }

            return TableType.FromTableTypeDescriptions(TableTypeDescription.Regular);
        }

        protected override Limit ParseLimit(string[] handLines)
        {
            //Limit line format:
            //<gametype>Holdem NL $0.02/$0.04</gametype>
            //Alternative1:
            //<gametype>Holdem NL 0.02/0.04</gametype>
            //<currency>USD</currency>
            string gameTypeLine = GetGameTypeLineFromHandLines(handLines);
            int limitStringBeginIndex = gameTypeLine.LastIndexOf(' ') + 1;
            int limitStringEndIndex = gameTypeLine.LastIndexOf('<') - 1;
            string limitString = gameTypeLine.Substring(limitStringBeginIndex,
                                                        limitStringEndIndex - limitStringBeginIndex + 1);

            char currencySymbol = limitString[0];
            Currency currency;

            switch (currencySymbol)
            {
                case '$':
                    currency = Currency.USD;
                    NumberFormatInfo = NumberFormatInfo_USD;
                    break;
                case '€':
                    currency = Currency.EURO;
                    NumberFormatInfo = NumberFormatInfo_EURO;
                    break;
                case '£':
                    currency = Currency.GBP;
                    NumberFormatInfo = NumberFormatInfo_GBP;
                    break;
                default:
                    string tagValue = GetCurrencyTagValue(handLines);
                    switch (tagValue)
                    {
                        case "USD":
                            currency = Currency.USD;
                            NumberFormatInfo = NumberFormatInfo_USD;
                            break;
                        case "GBP":
                            currency = Currency.GBP;
                            NumberFormatInfo = NumberFormatInfo_GBP;
                            break;
                        case "EUR":
                            currency = Currency.EURO;
                            NumberFormatInfo = NumberFormatInfo_EURO;
                            break;
                        default:
                            throw new LimitException(handLines[0], "Unrecognized currency symbol " + currencySymbol);
                    }
                    break;
            }

            int slashIndex = limitString.IndexOf('/');

            string smallString = limitString.Remove(slashIndex);
            decimal small = ParseDecimal(smallString);
 
            string bigString = limitString.Substring(slashIndex + 1);
            decimal big = ParseDecimal(bigString);

            return Limit.FromSmallBlindBigBlind(small, big, currency);
        }

        protected override Buyin ParseBuyin(string[] handLines)
        {
            throw new NotImplementedException();
        }

        private string GetCurrencyTagValue(string[] handLines)
        {
            for (int i = 0; i < 11; i++)
            {
                string handline = handLines[i];
                if (handline[1] == 'c' && handline[2] == 'u')
                {
                    int endIndex = handline.IndexOf('<', 10);
                    return handline.Substring(10, endIndex - 10);
                }
            }
            return "";
        }

        public override bool IsValidHand(string[] handLines)
        {
            //Check 1 - Are we in a Session Tag
            if (handLines[0].StartsWith("<session") == false ||
                handLines[handLines.Length - 1].StartsWith("</session") == false)
            {
                return false;
            }

            //Check 3 - Do we have between 2 and 10 players?
            string[] playerLines = GetPlayerLinesFromHandLines(handLines);
            if (playerLines.Count() < 2 || playerLines.Count() > 10)
            {
                return false;
            }

            //todo add more checks related to action parsing
            return true;
        }

        public override bool IsValidOrCancelledHand(string[] handLines, out bool isCancelled)
        {
            isCancelled = false;
            return IsValidHand(handLines);
        }

        protected override List<HandAction> ParseHandActions(string[] handLines, GameType gameType = GameType.Unknown)
        {
            List<HandAction> actions = new List<HandAction>();

            int startRow = GetRoundStart(handLines);// offset + playerLines.Length + 2;

            Street currentStreet = Street.Null;
            
            for (int i = startRow; i < handLines.Length - 2; i++)
            {
                string handLine = handLines[i].TrimStart();

                //If we are starting a new round, update the current street 
                if (handLine[1] == 'r')
                {
                    int roundNumber = GetRoundNumberFromLine(handLine);
                    switch (roundNumber)
                    {
                        case 0:
                        case 1:
                            currentStreet = Street.Preflop;
                            break;
                        case 2:
                            currentStreet = Street.Flop;
                            break;
                        case 3:
                            currentStreet = Street.Turn;
                            break;
                        case 4:
                            currentStreet = Street.River;
                            break;
                        default:
                            throw new Exception("Encountered unknown round number " + roundNumber);
                    }
                }
                //If we're an action, parse the action and act accordingly
                else if (handLine[1] == 'a')
                {
                    HandAction action = GetHandActionFromActionLine(handLine, currentStreet);                   
                    actions.Add(action);
                }
            }

            //Generate the show card + winnings actions
            actions.AddRange(GetWinningAndShowCardActions(handLines));

            // we need to fix dead money postings at the end
            return FixDeadMoneyPosting(actions);
        }

        private int GetRoundStart(string[] handLines)
        {
            for (int i = 0; i < handLines.Length; i++)
            {
                string line = handLines[i];

                if (line[1] == 'r')
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException("Round not Found");
        }

        private List<HandAction> FixDeadMoneyPosting(List<HandAction> actions)
        {
            // sort the actions, because regular SB + BB actions are always the first actions ( although might not be the first in the hand history )
            actions = actions.OrderBy(t => t.ActionNumber).ToList();
            var bigBlindValue = 0.0m;
            var looper = new List<HandAction>(actions);
            foreach (var action in looper)
            {
                if (action.HandActionType.Equals(HandActionType.BIG_BLIND))
                {
                    if (bigBlindValue == 0.0m)
                    {
                        bigBlindValue = Math.Abs(action.Amount);
                        continue;
                    }

                    if (Math.Abs(action.Amount) > bigBlindValue)
                    {
                        var newAction = new HandAction(action.PlayerName, HandActionType.POSTS, action.Amount, action.Street, action.ActionNumber);
                        actions.Remove(action);
                        actions.Add(newAction);
                    }
                }
            }

            return actions;
        }

        private List<HandAction> GetWinningAndShowCardActions(string[] handLines)
        {
            int actionNumber = Int32.MaxValue - 100;

            PlayerList playerList = ParsePlayers(handLines);

            List<HandAction> winningAndShowCardActions = new List<HandAction>();

            foreach (Player player in playerList)
            {
                if (player.hasHoleCards)
                {
                    HandAction showCardsAction = new HandAction(player.PlayerName, HandActionType.SHOW, 0, Street.Showdown, actionNumber++);    
                    winningAndShowCardActions.Add(showCardsAction);
                }                
            }

            string[] playerLines = GetPlayerLinesFromHandLines(handLines);
            for (int i = 0; i < playerLines.Length; i++)
            {
                string playerLine = playerLines[i];
                decimal winnings = GetWinningsFromPlayerLine(playerLine);                
                if (winnings > 0)
                {
                    string playerName = GetNameFromPlayerLine(playerLine);
                    WinningsAction winningsAction = new WinningsAction(playerName, HandActionType.WINS, winnings, 0, actionNumber++);
                    winningAndShowCardActions.Add(winningsAction);
                }
            }

            return winningAndShowCardActions;
        }

        private HandAction GetHandActionFromActionLine(string handLine, Street street)
        {
            int actionTypeNumber = GetActionTypeFromActionLine(handLine);
            string actionPlayerName = GetPlayerFromActionLine(handLine);
            decimal value = GetValueFromActionLine(handLine);
            int actionNumber = GetActionNumberFromActionLine(handLine);
            HandActionType actionType;
            switch (actionTypeNumber)
            {
                case 0:                
                    actionType = HandActionType.FOLD;
                    break;
                case 1:                 
                    actionType = HandActionType.SMALL_BLIND;
                    break;
                case 2:
                    actionType = HandActionType.BIG_BLIND;
                    break;
                case 3:                 
                    actionType = HandActionType.CALL;
                    break;
                case 4:                 
                    actionType = HandActionType.CHECK;
                    break;
                case 5:
                    actionType = HandActionType.BET;
                    break;
                case 6://Both are all-ins but don't know the difference between them
                case 7:
                    return new AllInAction(actionPlayerName, value, street, false, actionNumber);
                case 8: //Case 8 is when a player sits out at the beginning of a hand 
                case 9: //Case 9 is when a blind isn't posted - can be treated as sitting out
                    actionType = HandActionType.SITTING_OUT;
                    break;
                case 15:
                    actionType = HandActionType.ANTE;
                    break;
                case 23:                 
                    actionType = HandActionType.RAISE;
                    break;
                default:
                    throw new Exception(string.Format("Encountered unknown Action Type: {0} w/ line \r\n{1}", actionTypeNumber, handLine));
            }
            return new HandAction(actionPlayerName, actionType, value, street, actionNumber);
        }

        protected int GetRoundNumberFromLine(string handLine)
        {
            int startPos = handLine.IndexOf(" n", StringComparison.Ordinal) + 5;
            int endPos = handLine.IndexOf('"', startPos) - 1;
            string numString = handLine.Substring(startPos, endPos - startPos + 1);
            return Int32.Parse(numString);            
        }

        protected int GetActionNumberFromActionLine(string actionLine)
        {
            int actionStartPos = actionLine.IndexOf(" n", StringComparison.Ordinal) + 5;
            int actionEndPos = actionLine.IndexOf('"', actionStartPos) - 1;
            string actionNumString = actionLine.Substring(actionStartPos, actionEndPos - actionStartPos + 1);
            return Int32.Parse(actionNumString);
        }

        protected string GetPlayerFromActionLine(string actionLine)
        {
            int nameStartPos = actionLine.IndexOf(" p", StringComparison.Ordinal) + 9;
            int nameEndPos = actionLine.IndexOf('"', nameStartPos) - 1;
            string name = actionLine.Substring(nameStartPos, nameEndPos - nameStartPos + 1);
            return name;
        }

        protected decimal GetValueFromActionLine(string actionLine)
        {
            int startPos = actionLine.IndexOf(" s", StringComparison.Ordinal) + 6;
            int endPos = actionLine.IndexOf('"', startPos) - 1;
            string value = actionLine.Substring(startPos, endPos - startPos + 1);
            return ParseDecimal(value);
        }

        protected int GetActionTypeFromActionLine(string actionLine)
        {
            int actionStartPos = actionLine.IndexOf(" t", StringComparison.Ordinal) + 7;
            int actionEndPos = actionLine.IndexOf('"', actionStartPos) - 1;
            string actionNumString = actionLine.Substring(actionStartPos, actionEndPos - actionStartPos + 1);
            return Int32.Parse(actionNumString);
        }

        protected override PlayerList ParsePlayers(string[] handLines)
        {
            /*
                <player seat="3" name="RodriDiaz3" chips="$2.25" dealer="0" win="$0" bet="$0.08" rebuy="0" addon="0" />
                <player seat="8" name="Kristi48ru" chips="$6.43" dealer="1" win="$0.23" bet="$0.16" rebuy="0" addon="0" />
                or
                <player seat="5" name="player5" chips="$100000" dealer="0" win="$0" bet="$0" />
             */

            string[] playerLines = GetPlayerLinesFromHandLines(handLines);

            PlayerList playerList = new PlayerList();

            for (int i = 0; i < playerLines.Length; i++)
            {
                string playerName = GetNameFromPlayerLine(playerLines[i]);
                decimal stack = GetStackFromPlayerLine(playerLines[i]);
                int seat = GetSeatNumberFromPlayerLine(playerLines[i]);
                playerList.Add(new Player(playerName, stack, seat)
                                   {
                                       IsSittingOut = true
                                   });
            }

            var actionLines = handLines.Where(p => p.StartsWith("<act", StringComparison.Ordinal));
            foreach (var line in actionLines)
            {
                string player = GetPlayerFromActionLine(line);
                int type = GetActionNumberFromActionLine(line);

                if (type != 8)
                {
                    playerList[player].IsSittingOut = false;
                }
            }

            /* 
             * Grab known hole cards for players and add them to the player
             * <cards type="Pocket" player="pepealas5">CA CK</cards>
             */

            string[] cardLines = GetCardLinesFromHandLines(handLines);

            for (int i = 0; i < cardLines.Length; i++)
            {
                string line = cardLines[i];

                //Getting the cards Type
                int typeIndex = line.IndexOf("e=\"", 10, StringComparison.Ordinal) + 3;
                char typeChar = line[typeIndex];

                //We only care about Pocket Cards
                if (typeChar != 'P')
                {
                    continue;
                }

                //When players fold, we see a line: 
                //<cards type="Pocket" player="pepealas5">X X</cards>
                //or:
                //<cards type="Pocket" player="playername"></cards>
                //We skip these lines
                if (line[line.Length - 9] == 'X' || line[line.Length - 9] == '>')
                {
                    continue;
                }

                int playerNameStartIndex = line.IndexOf("r=\"", 10, StringComparison.Ordinal) + 3;
                int playerNameEndIndex = line.IndexOf('"', playerNameStartIndex) - 1;
                string playerName = line.Substring(playerNameStartIndex,
                                                       playerNameEndIndex - playerNameStartIndex + 1);
                Player player = playerList.First(p => p.PlayerName.Equals(playerName));


                int playerCardsStartIndex = line.LastIndexOf('>', line.Length - 11) + 1;
                int playerCardsEndIndex = line.Length - 9;
                string playerCardString = line.Substring(playerCardsStartIndex,
                                                        playerCardsEndIndex - playerCardsStartIndex + 1);

                ////To make sure we know the exact character location of each card, turn 10s into Ts (these are recognized by our parser)
                ////Had to change this to specific cases so we didn't accidentally change player names
                playerCardString = playerCardString.Replace("10", "T");
                string[] cards = playerCardString.Split(' ');
                if (cards.Length > 1)
                {
                    player.HoleCards = HoleCards.NoHolecards(player.PlayerName);
                    foreach (string card in cards)
                    {
                        //Suit and rank are reversed in these strings, so we flip them around before adding
                        player.HoleCards.AddCard(new Card(card[1], card[0]));
                    }
                }
            }

            return playerList;
        }

        protected override BoardCards ParseCommunityCards(string[] handLines)
        {
            List<Card> boardCards = new List<Card>();
            /* 
             * <cards type="Flop" player="">D6 S9 S7</cards>
             * <cards type="Turn" player="">H8</cards>
             * <cards type="River" player="">D5</cards>
             * <cards type="Pocket" player="pepealas5">CA CK</cards>
             */

            string[] cardLines = GetCardLinesFromHandLines(handLines);

            for (int i = 0; i < cardLines.Length; i++)
            {
                string handLine = cardLines[i];
                handLine = handLine.TrimStart();

                //To make sure we know the exact character location of each card, turn 10s into Ts (these are recognized by our parser)
                handLine = handLine.Replace("10", "T");

                //The suit/ranks are reversed, so we need to reverse them when adding them to our board card string
                int typeIndex = handLine.IndexOf("e=\"", StringComparison.Ordinal);
                char streetChar = handLine[typeIndex + 3];

                int cardsStartIndex = handLine.LastIndexOf('>', handLine.Length - 9) + 1;
                //Flop
                if (streetChar == 'F')
                {
                    boardCards.Add(new Card(handLine[cardsStartIndex + 1], handLine[cardsStartIndex]));
                    boardCards.Add(new Card(handLine[cardsStartIndex + 4], handLine[cardsStartIndex + 3]));
                    boardCards.Add(new Card(handLine[cardsStartIndex + 7], handLine[cardsStartIndex + 6]));
                }
                //Turn
                if (streetChar == 'T')
                {
                    boardCards.Add(new Card(handLine[cardsStartIndex + 1], handLine[cardsStartIndex]));
                }
                //River
                if (streetChar == 'R')
                {
                    boardCards.Add(new Card(handLine[cardsStartIndex + 1], handLine[cardsStartIndex]));
                    break;
                }
            }

            return BoardCards.FromCards(boardCards.ToArray());
        }

        static decimal ParseDecimal(string amountString)
        {
            string currencyRemoved = amountString.TrimStart('€', '$', '£');

            decimal amount = decimal.Parse(currencyRemoved, CultureInfo.InvariantCulture);

            return amount;
        }

        protected override string ParseHeroName(string[] handlines)
        {
            const string tag = "<nickname>";
            for (int i = 0; i < handlines.Length; i++)
            {
                if (handlines[i][1] == 'n' && handlines[i].StartsWith(tag))
                {
                    string line = handlines[i];
                    int startindex = tag.Length;
                    int endindex = line.IndexOf('<', startindex);
                    return line.Substring(startindex, endindex - startindex);
                }
            }
            return null;
        }
    }
}
