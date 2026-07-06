using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Frostvein.Parser.Import
{
    public class ImportCards : IImport
    {
        #region Members

        private readonly ImportConfiguration _configuration;

        #endregion

        #region Instantiation

        public ImportCards(ImportConfiguration config)
        {
            _configuration = config;
        }

        #endregion

        #region Methods

        public void Import()
        {
            var filePath = Path.Combine(_configuration.DatFolder, "Card.dat");

            var fileCardLang = Path.Combine(_configuration.LangFolder, $"_code_{_configuration.Lang}_Card.txt");

            var existingCards = DAOFactory.CardDAO.LoadAll().Select(x => x.CardId).ToHashSet();

            var card = new CardDTO();
            var cards = new List<CardDTO>();
            var bcards = new List<BCardDTO>();
            var dictionaryIdLang = new Dictionary<string, string>();

            var counter = 0;
            var itemAreaBegin = false;

            using (var npcIdLangStream = new StreamReader(fileCardLang, Encoding.GetEncoding(1252)))
            {
                string line;
                while ((line = npcIdLangStream.ReadLine()) != null)
                {
                    var linesave = line.Split('\t');
                    if (linesave.Length > 1 && !dictionaryIdLang.ContainsKey(linesave[0]))
                        dictionaryIdLang.Add(linesave[0], linesave[1]);
                }
            }

            using (var npcIdStream = new StreamReader(filePath, Encoding.GetEncoding(1252)))
            {
                string line;
                while ((line = npcIdStream.ReadLine()) != null)
                {
                    var currentLine = line.Split('\t');

                    if (currentLine.Length > 2 && currentLine[1] == "VNUM")
                    {
                        card = new CardDTO
                        {
                            CardId = Convert.ToInt16(currentLine[2])
                        };

                        DAOFactory.BCardDAO.DeleteByCardId(card.CardId);

                        if (!existingCards.Contains(card.CardId))
                        {
                            cards.Add(card);
                            counter++;
                            itemAreaBegin = true;
                        }
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "NAME")
                    {
                        if (!itemAreaBegin) continue;

                        card.Name = dictionaryIdLang.ContainsKey(currentLine[2])
                            ? dictionaryIdLang[currentLine[2]]
                            : "";
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "GROUP")
                    {
                        if (!itemAreaBegin) continue;

                        card.Level = Convert.ToByte(currentLine[3]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "EFFECT")
                    {
                        if (!itemAreaBegin) continue;

                        card.EffectId = Convert.ToInt32(currentLine[2]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "STYLE")
                    {
                        if (!itemAreaBegin) continue;

                        card.BuffType = (BuffType)Convert.ToByte(currentLine[3]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "TIME")
                    {
                        if (!itemAreaBegin) continue;

                        card.Duration = Convert.ToInt32(currentLine[2]);
                        card.Delay = Convert.ToInt32(currentLine[3]);
                    }
                    else
                    {
                        BCardDTO bcard;
                        if (currentLine.Length > 3 && currentLine[1] == "1ST")
                        {
                            for (var i = 0; i < 3; i++)
                            {
                                if (currentLine[2 + i * 6] == "-1" || currentLine[2 + i * 6] == "0") continue;

                                var first = int.Parse(currentLine[i * 6 + 6]);
                                bcard = new BCardDTO
                                {
                                    CardId = card.CardId,
                                    Type = byte.Parse(currentLine[2 + i * 6]),
                                    SubType = (byte)((Convert.ToByte(currentLine[3 + i * 6]) + 1) * 10 + 1 +
                                                      (first < 0 ? 1 : 0)),
                                    FirstData = (first > 0 ? first : -first) / 4,
                                    SecondData = int.Parse(currentLine[7 + i * 6]) / 4,
                                    ThirdData = int.Parse(currentLine[5 + i * 6]),
                                    IsLevelScaled = Convert.ToBoolean(first % 4),
                                    IsLevelDivided = Math.Abs(first % 4) == 2
                                };
                                bcards.Add(bcard);
                            }
                        }
                        else if (currentLine.Length > 3 && currentLine[1] == "2ST")
                        {
                            for (var i = 0; i < 2; i++)
                            {
                                if (currentLine[2 + i * 6] == "-1" || currentLine[2 + i * 6] == "0") continue;

                                var first = int.Parse(currentLine[i * 6 + 6]);
                                bcard = new BCardDTO
                                {
                                    CardId = card.CardId,
                                    Type = byte.Parse(currentLine[2 + i * 6]),
                                    SubType = (byte)((Convert.ToByte(currentLine[3 + i * 6]) + 1) * 10 + 1 +
                                                      (first < 0 ? 1 : 0)),
                                    FirstData = (first > 0 ? first : -first) / 4,
                                    SecondData = int.Parse(currentLine[7 + i * 6]) / 4,
                                    ThirdData = int.Parse(currentLine[5 + i * 6]),
                                    IsLevelScaled = Convert.ToBoolean(first % 4),
                                    IsLevelDivided = first % 4 == 2
                                };
                                bcards.Add(bcard);
                            }
                        }
                        else if (currentLine.Length > 3 && currentLine[1] == "LAST")
                        {
                            card.TimeoutBuff = short.Parse(currentLine[2]);
                            card.TimeoutBuffChance = byte.Parse(currentLine[3]);
                            itemAreaBegin = false;
                        }
                    }
                }

                BCardDTO returnBCard(short cardId, byte type, byte subType, int firstData, int secondData = 0,
                    int thirdData = 0, byte castType = 0, bool isLevelScaled = false, bool isLevelDivided = false)
                {
                    return new BCardDTO
                    {
                        CardId = cardId,
                        Type = type,
                        SubType = subType,
                        FirstData = firstData,
                        SecondData = secondData,
                        ThirdData = thirdData,
                        CastType = castType,
                        IsLevelScaled = isLevelScaled,
                        IsLevelDivided = isLevelDivided
                    };
                }

                bcards.Add(returnBCard(146, 44, 6, 50));
                bcards.Add(returnBCard(131, 8, 2, 30));
                bcards.Add(returnBCard(131, 8, 3, 30));
                bcards.Add(returnBCard(131, 8, 4, 30));
                bcards.Add(returnBCard(131, 8, 5, 30));

                DAOFactory.BCardDAO.DeleteByCardId(146);
                DAOFactory.BCardDAO.DeleteByCardId(131);

                DAOFactory.CardDAO.Insert(cards);
                DAOFactory.BCardDAO.Insert(bcards);

                Logger.Log.Info($"{counter} Cards parsed");
                Logger.Log.Info($"{bcards.Count} Cards BCARD parsed");
            }
        }

        #endregion
    }
}