using Parse = Sanderling.Parse;
using MemoryStruct = Sanderling.Interface.MemoryStruct;
using System.IO;

//TODO:
//TODO:
//Host.Break(); // Halts execution until user continues

//WARNING WARNING WARNING - Filter must be set to station only

var Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
string inputFileName = @"C:\Users\Jason\Documents\Visual Studio 2017\Projects\GitHub Eve\trunk\MarketLog.txt";
bool ContainsBlueBackground(MemoryStruct.IListEntry Entry)=>Entry?.ListBackgroundColor?.Any(BackgroundColor=>111 < BackgroundColor?.OMilli && 777 < BackgroundColor?.BMilli && BackgroundColor?.RMilli < 111 && BackgroundColor?.GMilli < 111) ?? false;
bool ContainsGreenBackground(MemoryStruct.IListEntry Entry)=>Entry?.ListBackgroundColor?.Any(BackgroundColor=>111 < BackgroundColor?.OMilli && 777 < BackgroundColor?.GMilli && BackgroundColor?.RMilli < 111 && BackgroundColor?.BMilli < 111) ?? false;
bool ContainsBlackBackground(MemoryStruct.IListEntry Entry)=>Entry?.ListBackgroundColor?.Any(BackgroundColor=>BackgroundColor?.OMilli > 450 && BackgroundColor?.BMilli > 240 && BackgroundColor?.RMilli > 240 && BackgroundColor?.GMilli > 240) ?? true;
bool MatchingOrder(MemoryStruct.IListEntry Entry)=>Entry?.LabelText?.Any(someText=>someText.Text.ToString().RegexMatchSuccess(orderName)) ?? false;
List<FileOrderEntry> fileOrderEntries = new List<FileOrderEntry>();
Random rnd = new Random();
IWindow ModalUIElement=>Measurement?.EnumerateReferencedUIElementTransitive()?.OfType<IWindow>()?.Where(window=>window?.isModal ?? false)?.OrderByDescending(window=>window?.InTreeIndex ?? int.MinValue)?.FirstOrDefault();

string orderName = "";
double defaultMargin = 5.0;

using(FileStream fileStream = new FileStream(inputFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
  using(var reader = new StreamReader(fileStream)) {
    while (!reader.EndOfStream) {
      var line = reader.ReadLine();
      var values = line.Split(',');
      try {
        if (!values[0].ToString().Equals("Blank")) {
          double minPrice = CalcMinPrice(Convert.ToDouble(values[2]), Convert.ToDouble(values[3]), Convert.ToDouble(values[5]));
          double maxPrice = CalcMaxPrice(Convert.ToDouble(values[2]), Convert.ToDouble(values[4]), Convert.ToDouble(values[5]));

          FileOrderEntry newFileOrder = new FileOrderEntry(values[0].ToString(), values[1].ToString(), Convert.ToDouble(values[2]), minPrice, maxPrice, Convert.ToDouble(values[5]), Convert.ToInt32(values[6]), Convert.ToDouble(values[7]), Convert.ToDateTime(values[8]));
          fileOrderEntries.Add(newFileOrder);
        }
      }
      catch {
        Host.Log("Couldn't read: " + line.ToString());
      }
    }
  }
}


public class FileOrderEntry {
  public string Name {
    get;
    set;
  }
  public string Type {
    get;
    set;
  }
  public double StartPrice {
    get;
    set;
  }
  public double LowestPrice {
    get;
    set;
  }
  public double HighestPrice {
    get;
    set;
  }
  public double Margin {
    get;
    set;
  }
  public int NumOfPriceChanges {
    get;
    set;
  }
  public double PriceChangeTotalCost {
    get;
    set;
  }
  public DateTime UpdateTime {
    get;
    set;
  }

  public FileOrderEntry(string name, string type, double startPrice, double lowestPrice, double highestPrice, double margin, int numOfPriceChanges, double priceChangeTotalCost, DateTime updateTime) {
    Name = name;
    Type = type;
    StartPrice = startPrice;
    LowestPrice = lowestPrice;
    HighestPrice = highestPrice;
    Margin = margin;
    NumOfPriceChanges = numOfPriceChanges;
    PriceChangeTotalCost = priceChangeTotalCost;
    UpdateTime = updateTime;
  }
}

double CalcMinPrice(double startPrice, double lowestPrice, double margin) {
  //If no minimum price set use margin value
  double answer = lowestPrice;
  if (lowestPrice < 0.01) {
    answer = Math.Round(startPrice * (1 - (margin / 100)), 2);
  }
  return answer;
}

double CalcMaxPrice(double startPrice, double highestPrice, double margin) {
  //If no maximum price set use margin value
  double answer = highestPrice;
  if (highestPrice < 0.01) {
    answer = Math.Round(startPrice * (1 + (margin / 100)), 2);
  }
  return answer;
}

bool ClickMenuEntryOnMenuRootJason(IUIElement MenuRoot, string MenuEntryRegexPattern) {
  int retryCount = 0;
  bool success = true;

  if (MenuRoot == null) {
    Host.Log("Fatal: Right click menu item is null");
  }

  Sanderling.MouseClickRight(MenuRoot);
  Host.Delay(1000);
  var Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
  var Menu = Measurement?.Menu?.FirstOrDefault();
  var MenuEntry = Menu?.EntryFirstMatchingRegexPattern(MenuEntryRegexPattern, RegexOptions.IgnoreCase);

  while (MenuEntry == null) {
    Sanderling.MouseClickRight(MenuRoot);
    Host.Delay(2000);
    Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
    Menu = Measurement?.Menu?.FirstOrDefault();
    MenuEntry = Menu?.EntryFirstMatchingRegexPattern(MenuEntryRegexPattern, RegexOptions.IgnoreCase);
    if (retryCount++>10) {
      success = false;
      break;
    }
  }

  Sanderling.MouseClickLeft(MenuEntry);
  return success;
}

void ClickMenuEntryOnMenuRoot(IUIElement MenuRoot, string MenuEntryRegexPattern) {
  Sanderling.MouseClickRight(MenuRoot);

  Host.Delay(1000);

  var Menu = Measurement?.Menu?.FirstOrDefault();

  var MenuEntry = Menu?.EntryFirstMatchingRegexPattern(MenuEntryRegexPattern, RegexOptions.IgnoreCase);

  Sanderling.MouseClickLeft(MenuEntry);
}

void EnterPrice(double price) {
  var portionInteger = (long) price;
  var cent = ((long)(price * 100)) % 100;
  Sanderling.TextEntry(portionInteger.ToString());
  if (0 < cent) {
    Sanderling.KeyboardPress(VirtualKeyCode.DECIMAL); // use OEM_COMMA if your locale setting has comma as decimal separator.
    Sanderling.TextEntry(cent.ToString("D2"));
  }
}

void CloseModalUIElementYes() {
  var ButtonClose = ModalUIElement?.ButtonText?.FirstOrDefault(button=>(button?.Text).RegexMatchSuccessIgnoreCase("yes"));

  Sanderling.MouseClickLeft(ButtonClose);
}

for (;;) {

  //Compare the entries in my orders to the object add if not in there.
  //What margin to use?
  var myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
  if(myOrders?.BuyOrderView != null && myOrders?.SellOrderView != null) {
    try {
      //If the market is open
      MemoryStruct.MarketOrderEntry[] buyOrdersInGame = myOrders?.BuyOrderView?.Entry?.ToArray();
      MemoryStruct.MarketOrderEntry[] sellOrdersInGame = myOrders?.SellOrderView?.Entry?.ToArray();
      int buyOrderCountInGame = buyOrdersInGame.Length;
      int sellOrderCountInGame = sellOrdersInGame.Length;
      if(buyOrderCountInGame > 0) {
        foreach(MemoryStruct.MarketOrderEntry buyOrderInGame in buyOrdersInGame) {
          string orderText = orderEntry?.LabelText[0].Text.ToString();
          string[] orderTextSplit = Regex.Split(orderText, @"<t>");
          string gameOrderName = orderTextSplit[0];
          bool foundName = false;
          foreach(FileOrderEntry fileOrderEntry in fileOrderEntries) {
            if(fileOrderEntry.Name.Equals(gameOrderName) && fileOrderEntry.Type.Equals("Buy Order")) {
              foundName = true;
              break;
            }
          }
          if(!foundName) {
            // Add details to FileOrderEntry object
            string orderPrice = orderTextSplit[2].Value.Substring(0, orderTextSplit[2].Value.Length - 4);
            orderPrice = orderPrice.Replace(@",", "");
            double orderPriceDbl = Convert.ToDouble(orderPrice);
            double minPrice = CalcMinPrice(orderPrice, 0.0, defaultMargin);
            double maxPrice = CalcMaxPrice(orderPrice, 0.0, defaultMargin);
            
            FileOrderEntry newFileOrder = new FileOrderEntry(gameOrderName, "Buy Order", orderPriceDbl, minPrice, maxPrice, defaultMargin, 0, 0.0, DateTime.Now);
            fileOrderEntries.Add(newFileOrder);
    
            //Print details for checking and pause.
            Host.Log("Name: " + newFileOrder.Name + "  Price: " + newFileOrder.StartPrice + "  Type: " + newFileOrder.Type + "  Min Price: " + newFileOrder.LowestPrice);
            Host.Break();
          }
        }
      }
      if(sellOrderCountInGame > 0) {
      }
    }
  } catch {/*Don't care if this fails*/}
  
  Something_has_gone_wrong: //label to jump back to if something goes wrong

  foreach(FileOrderEntry fileOrderEntry in fileOrderEntries) {

    //If five mins and 20s has passed since last update then process again
    bool timeToCheck = false;
    DateTime a = Convert.ToDateTime(@fileOrderEntry.UpdateTime);
    DateTime b = DateTime.Now;
    if (Math.Round(b.Subtract(a).TotalSeconds, 0) > 320) {
      timeToCheck = true;
      Host.Log("Time to check: " + fileOrderEntry.Name + " - " + fileOrderEntry.Type);
    }

    if (timeToCheck) {

      //Ensure Market Window is open
      while (null == Measurement?.WindowRegionalMarket) {
        Host.Log("Open Market");
        if (null == Measurement?.WindowRegionalMarket) {
          Sanderling.MouseClickLeft(Measurement?.Neocom?.MarketButton);
        }
        Host.Delay(2000);
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
      }

      //Wait for My Orders Button to appear
      while (Measurement?.WindowRegionalMarket?.FirstOrDefault()?.RightTabGroup?.ListTab[2]?.RegionInteraction == null) {
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        Host.Delay(500);
      }

      //Click My Orders
      Sanderling.MouseClickLeft(Measurement?.WindowRegionalMarket?.FirstOrDefault()?.RightTabGroup?.ListTab[2]?.RegionInteraction);

      //Wait for MyOrders to be populated
      Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
      myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
      while (myOrders == null) {
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        Sanderling.MouseClickLeft(Measurement?.WindowRegionalMarket?.FirstOrDefault()?.RightTabGroup?.ListTab[2]?.RegionInteraction);
        myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
        Host.Delay(1000);
      }

      var orderSectionMyOrders = myOrders?.BuyOrderView;
      if (fileOrderEntry.Type.Equals("Sell Order")) {
        orderSectionMyOrders = myOrders?.SellOrderView;
      }

      //Make sure Buy/Sell section is visible
      while (orderSectionMyOrders == null) {
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
        orderSectionMyOrders = myOrders?.BuyOrderView;
        if (fileOrderEntry.Type.Equals("Sell Order")) {
          orderSectionMyOrders = myOrders?.SellOrderView;
        }
        Host.Delay(500);

        while (orderSectionMyOrders == null) {
          Host.Delay(500);
          Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        }
      }

      //Find an order in the list that matches order read from file and View Market Details
      orderName = fileOrderEntry.Name.ToString();
      var getMatchingOrder = orderSectionMyOrders?.Entry?.FirstOrDefault(MatchingOrder);

      bool foundOrder = false;
      if (getMatchingOrder != null) {
        foundOrder = true;
      } else {
        Host.Delay(1000);
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
        orderSectionMyOrders = myOrders?.BuyOrderView;
        if (fileOrderEntry.Type.Equals("Sell Order")) {
          orderSectionMyOrders = myOrders?.SellOrderView;
        }
        getMatchingOrder = orderSectionMyOrders?.Entry?.FirstOrDefault(MatchingOrder);
        if (getMatchingOrder != null) {
          foundOrder = true;
          break;
        }
        Host.Delay(1000);
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
        orderSectionMyOrders = myOrders?.BuyOrderView;
        if (fileOrderEntry.Type.Equals("Sell Order")) {
          orderSectionMyOrders = myOrders?.SellOrderView;
        }
        getMatchingOrder = orderSectionMyOrders?.Entry?.FirstOrDefault(MatchingOrder);
        if (getMatchingOrder != null) {
          foundOrder = true;
          break;
        }
        Host.Delay(1000);
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
        orderSectionMyOrders = myOrders?.BuyOrderView;
        if (fileOrderEntry.Type.Equals("Sell Order")) {
          orderSectionMyOrders = myOrders?.SellOrderView;
        }
        getMatchingOrder = orderSectionMyOrders?.Entry?.FirstOrDefault(MatchingOrder);
        if (getMatchingOrder != null) {
          foundOrder = true;
          break;
        }
        Host.Delay(1000);
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
        orderSectionMyOrders = myOrders?.BuyOrderView;
        if (fileOrderEntry.Type.Equals("Sell Order")) {
          orderSectionMyOrders = myOrders?.SellOrderView;
        }
        getMatchingOrder = orderSectionMyOrders?.Entry?.FirstOrDefault(MatchingOrder);
        if (getMatchingOrder != null) {
          foundOrder = true;
          break;
        }
        Host.Delay(1000);
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
        orderSectionMyOrders = myOrders?.BuyOrderView;
        if (fileOrderEntry.Type.Equals("Sell Order")) {
          orderSectionMyOrders = myOrders?.SellOrderView;
        }
        getMatchingOrder = orderSectionMyOrders?.Entry?.FirstOrDefault(MatchingOrder);
        if (getMatchingOrder != null) {
          foundOrder = true;
          break;
        }
        Host.Delay(1000);
        Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
        myOrders = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.MyOrders;
        orderSectionMyOrders = myOrders?.BuyOrderView;
        if (fileOrderEntry.Type.Equals("Sell Order")) {
          orderSectionMyOrders = myOrders?.SellOrderView;
        }
        getMatchingOrder = orderSectionMyOrders?.Entry?.FirstOrDefault(MatchingOrder);
        if (getMatchingOrder != null) {
          foundOrder = true;
          break;
        }
      }
      if (foundOrder == false) {
        Host.Log("Warning: Can't find order " + orderName);
        Console.Beep(500, 1000);
        Host.Delay(500);
        Console.Beep(500, 1000);
        Host.Delay(500);
        Console.Beep(500, 1000);
      } else {

        if (!ClickMenuEntryOnMenuRootJason(getMatchingOrder, "View Market")) {
          Host.Log("Failed View Market Details");
          goto Something_has_gone_wrong;
        }
        Host.Delay(2000);

        int retryCount = 0;
        while (Measurement?.WindowRegionalMarket ? [0]?.SelectedItemTypeDetails?.MarketData?.BuyerView == null && Measurement?.WindowRegionalMarket ? [0]?.SelectedItemTypeDetails?.MarketData?.SellerView == null) {
          if(retryCount++ > 30) {
            Host.Log("Failed View Market Details");
            goto Something_has_gone_wrong;
          }
          Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
          Host.Delay(200);
        }

        var orderSectionMarketData = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.SelectedItemTypeDetails?.MarketData?.BuyerView;
        if (fileOrderEntry.Type.Equals("Sell Order")) {
          orderSectionMarketData = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.SelectedItemTypeDetails?.MarketData?.SellerView;
        }
        while (orderSectionMarketData == null) {
          Host.Delay(500);
          Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
          orderSectionMarketData = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.SelectedItemTypeDetails?.MarketData?.BuyerView;
          if (fileOrderEntry.Type.Equals("Sell Order")) {
            orderSectionMarketData = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.SelectedItemTypeDetails?.MarketData?.SellerView;
          }
        }
        while (orderSectionMarketData?.Entry?.FirstOrDefault() == null) {
          Host.Delay(500);
          Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
          orderSectionMarketData = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.SelectedItemTypeDetails?.MarketData?.BuyerView;
          if (fileOrderEntry.Type.Equals("Sell Order")) {
            orderSectionMarketData = Measurement?.WindowRegionalMarket?.FirstOrDefault()?.SelectedItemTypeDetails?.MarketData?.SellerView;
          }
        }

        //TODO: Split here for buy sell
        if (fileOrderEntry.Type.Equals("Sell Order")) {

          //Seems like a bug means that BackgroundColor is not populated on Seller unless the entry is clicked first
          Sanderling.MouseClickLeft(orderSectionMarketData?.Entry?.FirstOrDefault());
          Host.Delay(5000);

          var FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
          var FirstBlack = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlackBackground);
          Host.Log("FirstBlue: " + string.Join(", ", FirstBlue?.ListColumnCellLabel?.Select(ColumnCellLabel=>ColumnCellLabel.Key?.Text + " : " + ColumnCellLabel.Value)?.ToArray() ?? new string[0]));
          Host.Log("FirstBlack: " + string.Join(", ", FirstBlack?.ListColumnCellLabel?.Select(ColumnCellLabel=>ColumnCellLabel.Key?.Text + " : " + ColumnCellLabel.Value)?.ToArray() ?? new string[0]));

          bool foundBlue = false;
          if (FirstBlue != null) {
            foundBlue = true;
          } else {
            //Check multiple times to be sure
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
          }
          if (foundBlue == false) {
            Host.Log("No Blue!");
            Console.Beep(500, 1000);
            Host.Delay(500);
            Console.Beep(500, 1000);
            Host.Delay(500);
            Console.Beep(500, 1000);
          }

          bool foundBlack = false;
          if (FirstBlack != null) {
            foundBlack = true;
          } else {
            //Check multiple times to be sure
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlack = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlackBackground);
            if (FirstBlack != null) {
              foundBlack = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlack = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlackBackground);
            if (FirstBlack != null) {
              foundBlack = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlack = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlackBackground);
            if (FirstBlack != null) {
              foundBlack = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlack = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlackBackground);
            if (FirstBlack != null) {
              foundBlack = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlack = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlackBackground);
            if (FirstBlack != null) {
              foundBlack = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlack = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlackBackground);
            if (FirstBlack != null) {
              foundBlack = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlack = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlackBackground);
            if (FirstBlack != null) {
              foundBlack = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlack = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlackBackground);
            if (FirstBlack != null) {
              foundBlack = true;
              break;
            }
          }

          if (foundBlack == false) {
            Host.Log("No Black!");
          }

          if (foundBlack == true && foundBlue == true) {
            var bluePriceArray = FirstBlue?.ListColumnCellLabel?.ToArray();
            string bluePriceStr = bluePriceArray[2].Value.Substring(0, bluePriceArray[2].Value.Length - 4);
            bluePriceStr = bluePriceStr.Replace(@",", "");
            double bluePriceDbl = Convert.ToDouble(bluePriceStr);

            var BlackPriceArray = FirstBlack?.ListColumnCellLabel?.ToArray();
            string BlackPriceStr = BlackPriceArray[2].Value.Substring(0, BlackPriceArray[2].Value.Length - 4);
            BlackPriceStr = BlackPriceStr.Replace(@",", "");
            double BlackPriceDbl = Convert.ToDouble(BlackPriceStr);

            double newBluePrice = 0;
            if (bluePriceDbl - BlackPriceDbl > 0.005) {
              newBluePrice = BlackPriceDbl - 0.02;
              if (newBluePrice < fileOrderEntry.LowestPrice) {
                //Price gone too low
                newBluePrice = 0;
              }
            }

            if (newBluePrice > 0) {
              if (!ClickMenuEntryOnMenuRootJason(FirstBlue, "Modify Order")) {
                Host.Log("Failed to Modify Order Sell");
                goto Something_has_gone_wrong;
              }
              Host.Delay(2000);
              EnterPrice(newBluePrice);
              //Verify entered value
              Sanderling.InvalidateMeasurement();
              Host.Delay(2000);
              Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
              string enteredNewValueStr = Measurement?.WindowMarketAction?.FirstOrDefault()?.InputText?.FirstOrDefault()?.Text?.ToString();
              enteredNewValueStr = enteredNewValueStr.Replace(@",", "");
              double enteredNewValueDbl = Convert.ToDouble(enteredNewValueStr);

              var actionArray = Measurement?.WindowMarketAction?.FirstOrDefault()?.LabelText?.ToArray();
              string priceChangeStr = actionArray ? [12]?.Text.ToString();
              priceChangeStr = priceChangeStr.Substring(0, priceChangeStr.Length - 4);
              priceChangeStr = priceChangeStr.Replace(@",", "");
              double priceChangeDbl = Convert.ToDouble(priceChangeStr);
              priceChangeDbl = Math.Round(priceChangeDbl, 2);

              string brokerFeeStr = actionArray ? [14]?.Text.ToString();
              brokerFeeStr = brokerFeeStr.Substring(0, brokerFeeStr.Length - 4);
              brokerFeeStr = brokerFeeStr.Replace(@",", "");
              double brokerFeeDbl = Convert.ToDouble(brokerFeeStr);
              brokerFeeDbl = Math.Round(brokerFeeDbl, 2);

              Host.Log("Entry: " + fileOrderEntry.Name + " New Price: " + newBluePrice + " Entered Price: " + enteredNewValueDbl + " Max Price: " + fileOrderEntry.HighestPrice.ToString() + " Price Change: " + (priceChangeDbl + brokerFeeDbl));

              if (Math.Abs(newBluePrice - enteredNewValueDbl) < 0.011) {
                //price is as expected so click ok
                var ButtonOK = Measurement?.WindowMarketAction?.FirstOrDefault()?.ButtonText?.FirstOrDefault(button=>(button?.Text).RegexMatchSuccessIgnoreCase("ok"));
                Sanderling.MouseClickLeft(ButtonOK);
                Host.Delay(5000);
                Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
                CloseModalUIElementYes();
                CloseModalUIElementYes();
                CloseModalUIElementYes();
                fileOrderEntry.NumOfPriceChanges = fileOrderEntry.NumOfPriceChanges + 1;
                fileOrderEntry.UpdateTime = DateTime.Now;
                fileOrderEntry.PriceChangeTotalCost = fileOrderEntry.PriceChangeTotalCost + priceChangeDbl + brokerFeeDbl;
              }
            } else {
              Host.Log("No change needed for " + orderName + " - " + fileOrderEntry.Type);
            }
          }
        } else {
          var FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
          var FirstGreen = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsGreenBackground);
          Host.Log("FirstBlue: " + string.Join(", ", FirstBlue?.ListColumnCellLabel?.Select(ColumnCellLabel=>ColumnCellLabel.Key?.Text + " : " + ColumnCellLabel.Value)?.ToArray() ?? new string[0]));
          Host.Log("FirstGreen: " + string.Join(", ", FirstGreen?.ListColumnCellLabel?.Select(ColumnCellLabel=>ColumnCellLabel.Key?.Text + " : " + ColumnCellLabel.Value)?.ToArray() ?? new string[0]));

          bool foundBlue = false;
          if (FirstBlue != null) {
            foundBlue = true;
          } else {
            //Check multiple times to be sure
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstBlue = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsBlueBackground);
            if (FirstBlue != null) {
              foundBlue = true;
              break;
            }
          }
          if (foundBlue == false) {
            Host.Log("No Blue!");
            Console.Beep(500, 1000);
            Host.Delay(500);
            Console.Beep(500, 1000);
            Host.Delay(500);
            Console.Beep(500, 1000);
          }

          bool foundGreen = false;
          if (FirstGreen != null) {
            foundGreen = true;
          } else {
            //Check multiple times to be sure
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstGreen = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsGreenBackground);
            if (FirstGreen != null) {
              foundGreen = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstGreen = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsGreenBackground);
            if (FirstGreen != null) {
              foundGreen = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstGreen = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsGreenBackground);
            if (FirstGreen != null) {
              foundGreen = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstGreen = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsGreenBackground);
            if (FirstGreen != null) {
              foundGreen = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstGreen = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsGreenBackground);
            if (FirstGreen != null) {
              foundGreen = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstGreen = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsGreenBackground);
            if (FirstGreen != null) {
              foundGreen = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstGreen = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsGreenBackground);
            if (FirstGreen != null) {
              foundGreen = true;
              break;
            }
            Host.Delay(1000);
            Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
            FirstGreen = orderSectionMarketData?.Entry?.FirstOrDefault(ContainsGreenBackground);
            if (FirstGreen != null) {
              foundGreen = true;
              break;
            }
          }

          if (foundGreen == false) {
            Host.Log("No Green!");
          }

          if (foundGreen == true && foundBlue == true) {
            var bluePriceArray = FirstBlue?.ListColumnCellLabel?.ToArray();
            string bluePriceStr = bluePriceArray[2].Value.Substring(0, bluePriceArray[2].Value.Length - 4);
            bluePriceStr = bluePriceStr.Replace(@",", "");
            double bluePriceDbl = Convert.ToDouble(bluePriceStr);

            var greenPriceArray = FirstGreen?.ListColumnCellLabel?.ToArray();
            string greenPriceStr = greenPriceArray[2].Value.Substring(0, greenPriceArray[2].Value.Length - 4);
            greenPriceStr = greenPriceStr.Replace(@",", "");
            double greenPriceDbl = Convert.ToDouble(greenPriceStr);
            Host.Log("Price Diff: " + (greenPriceDbl - bluePriceDbl));
            double newBluePrice = 0;
            if (Math.Abs(greenPriceDbl - bluePriceDbl) < 0.001 || greenPriceDbl - bluePriceDbl > 0.005) {
              newBluePrice = greenPriceDbl + 0.02;
              if (newBluePrice > fileOrderEntry.HighestPrice) {
                //Price gone too high see if we can get next best slot
                newBluePrice = 0;
                var entryArray = orderSectionMarketData?.Entry?.ToArray();
                foreach(var entry in entryArray) {
                  var entryPriceArray = entry?.ListColumnCellLabel?.ToArray();
                  string entryPrice = entryPriceArray[2].Value.Substring(0, entryPriceArray[2].Value.Length - 4);
                  entryPrice = entryPrice.Replace(@",", "");
                  double entryPriceDbl = Convert.ToDouble(entryPrice);
                  if (entryPriceDbl < fileOrderEntry.HighestPrice - 1 && entryPriceDbl > bluePriceDbl) {
                    newBluePrice = entryPriceDbl + 0.02;
                    break;
                  }
                }
              }
            }

            if (newBluePrice > 0) {
              if (!ClickMenuEntryOnMenuRootJason(FirstBlue, "Modify Order")) {
                Host.Log("Failed to Modify Order Buy");
                goto Something_has_gone_wrong;
              }
              Host.Delay(2000);
              EnterPrice(newBluePrice);
              //Verify entered value
              Sanderling.InvalidateMeasurement();
              Host.Delay(2000);
              Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
              string enteredNewValueStr = Measurement?.WindowMarketAction?.FirstOrDefault()?.InputText?.FirstOrDefault()?.Text?.ToString();
              enteredNewValueStr = enteredNewValueStr.Replace(@",", "");
              double enteredNewValueDbl = Convert.ToDouble(enteredNewValueStr);

              var actionArray = Measurement?.WindowMarketAction?.FirstOrDefault()?.LabelText?.ToArray();
              string priceChangeStr = actionArray ? [12]?.Text.ToString();
              priceChangeStr = priceChangeStr.Substring(0, priceChangeStr.Length - 4);
              priceChangeStr = priceChangeStr.Replace(@",", "");
              double priceChangeDbl = Convert.ToDouble(priceChangeStr);
              priceChangeDbl = Math.Round(priceChangeDbl, 2);

              string brokerFeeStr = actionArray ? [14]?.Text.ToString();
              brokerFeeStr = brokerFeeStr.Substring(0, brokerFeeStr.Length - 4);
              brokerFeeStr = brokerFeeStr.Replace(@",", "");
              double brokerFeeDbl = Convert.ToDouble(brokerFeeStr);
              brokerFeeDbl = Math.Round(brokerFeeDbl, 2);

              Host.Log("Entry: " + fileOrderEntry.Name + " New Price: " + newBluePrice + " Entered Price: " + enteredNewValueDbl + " Max Price: " + fileOrderEntry.HighestPrice.ToString() + " Price Change: " + (priceChangeDbl + brokerFeeDbl));

              if (Math.Abs(newBluePrice - enteredNewValueDbl) < 0.011) {
                //price is as expected so click ok
                var ButtonOK = Measurement?.WindowMarketAction?.FirstOrDefault()?.ButtonText?.FirstOrDefault(button=>(button?.Text).RegexMatchSuccessIgnoreCase("ok"));
                Sanderling.MouseClickLeft(ButtonOK);
                Host.Delay(5000);
                Measurement = Sanderling?.MemoryMeasurementParsed?.Value;
                CloseModalUIElementYes();
                CloseModalUIElementYes();
                CloseModalUIElementYes();
                fileOrderEntry.NumOfPriceChanges = fileOrderEntry.NumOfPriceChanges + 1;
                fileOrderEntry.UpdateTime = DateTime.Now;
                fileOrderEntry.PriceChangeTotalCost = fileOrderEntry.PriceChangeTotalCost + priceChangeDbl + brokerFeeDbl;
              }
            } else {
              Host.Log("No change needed for " + orderName + " - " + fileOrderEntry.Type);
            }
          }
        }
      }
      //Click My Orders
      Sanderling.MouseClickLeft(Measurement?.WindowRegionalMarket?.FirstOrDefault()?.RightTabGroup?.ListTab[2]?.RegionInteraction);

      //backup input file & save contents of object
      Host.Log("Writing log.");
      System.IO.File.Copy(inputFileName, inputFileName + ".bak", true);
      using(FileStream fileStream = new FileStream(inputFileName, FileMode.Create, FileAccess.ReadWrite)) {
        using(var writer = new StreamWriter(fileStream)) {
          List<FileOrderEntry> SortedList = fileOrderEntries.OrderBy(o=>o.UpdateTime).ToList();
          foreach(FileOrderEntry fileOrderEntryToWrite in SortedList) {
            string output = fileOrderEntryToWrite.Name.ToString() + "," + fileOrderEntryToWrite.Type.ToString() + "," + fileOrderEntryToWrite.StartPrice.ToString() + "," + fileOrderEntryToWrite.LowestPrice.ToString() + "," + fileOrderEntryToWrite.HighestPrice.ToString() + "," + fileOrderEntryToWrite.Margin.ToString() + "," + fileOrderEntryToWrite.NumOfPriceChanges.ToString() + "," + fileOrderEntryToWrite.PriceChangeTotalCost.ToString() + "," + fileOrderEntryToWrite.UpdateTime.ToString();
            writer.WriteLine(output);
          }
        }
      }
    }
  }

  //Check every few seconds for item to be checked
  int delay = rnd.Next(2423, 5624);
  Host.Delay(delay);
}
