﻿using FilmSearchBot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class Program
{
    public static async Task Main(string[] args)
    {
        var botClient = new TelegramBotClient("6666617530:AAEc5I4KUCpYe1JHw2KM4g0AD9GGSvxDxb0");

        using CancellationTokenSource cts = new();

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        cts.Cancel();

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => HandleMessageAsync(botClient, update, cancellationToken),
                UpdateType.CallbackQuery => HandleCallBackQueryAsync(botClient, update, cancellationToken),
                //Yana update larni davom ettirib tutishingiz mumkin
            };

            try
            {
                await handler;
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Xato:{ex.Message}");
            }
        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }

    private static async Task HandleCallBackQueryAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private static async Task HandleMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        Console.WriteLine($"Received a '{messageText}' message in chat {update.Message.Chat.Id}.");

        var root = await Broker.GetFilmList(messageText);

        await SendFilmListAsync(messageText,botClient, update, cancellationToken);
    }

    private static async Task SendFilmListAsync(string messageText,ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var root = await Broker.GetFilmList(messageText);
        var listOfSearch = root.Search;
        if (listOfSearch == null)
            return;

        var page = root.PageNumber;
        var searchKey = root.SearchKey;

        int count = 0;
        var listOfInlineKeyboardButton = new List<List<InlineKeyboardButton>>();
        var inlineKeyBoardButtonsRow = new List<InlineKeyboardButton>();


        //agar 5dan kichik bo'lsa bir qator
        if (listOfSearch.Count <= 5)
        {
            inlineKeyBoardButtonsRow = new List<InlineKeyboardButton>();
            for (int j = 1; j <= listOfSearch.Count; j++)
            {
                inlineKeyBoardButtonsRow.Add(InlineKeyboardButton.WithCallbackData($"{count + 1}", $"{listOfSearch[count].imdbID}"));
                count++;
            }
            listOfInlineKeyboardButton.Add(inlineKeyBoardButtonsRow);
        }
        else//agar 5dan katta bo'lsa 2 qator
        {
            inlineKeyBoardButtonsRow = new List<InlineKeyboardButton>();
            for (int j = 1; j <= 5; j++)
            {
                inlineKeyBoardButtonsRow.Add(InlineKeyboardButton.WithCallbackData($"{count + 1}", $"{listOfSearch[count].imdbID}"));
                count++;
            }
            listOfInlineKeyboardButton.Add(inlineKeyBoardButtonsRow);

            inlineKeyBoardButtonsRow = new List<InlineKeyboardButton>();
            for (int j = 1; j <= listOfSearch.Count - 5; j++)
            {
                inlineKeyBoardButtonsRow.Add(InlineKeyboardButton.WithCallbackData($"{count + 1}", $"{listOfSearch[count].imdbID}"));
                count++;
            }
            listOfInlineKeyboardButton.Add(inlineKeyBoardButtonsRow);
        }

        //pagination uchun yana bir qator 
        //shu yerga paginationni oxirgi pagemi yo'qmi biladigan code yozish kerak 
        listOfInlineKeyboardButton.Add(new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData($"⬅️", $"page={page-1} {searchKey}"),
            InlineKeyboardButton.WithCallbackData($"➡️", $"page={page+1} {searchKey}"),
        });

        var inlineKeyboard = new InlineKeyboardMarkup(listOfInlineKeyboardButton);

        var TitleOfFilms = $"<b>Films page:{page} <i>(search:{searchKey} total result:{root.totalResults})</i>:</b>";
        for (int i = 0; i < listOfSearch.Count; i++)
        {
            TitleOfFilms += $"\n{i + 1}.<i>{listOfSearch[i].Title}-{listOfSearch[i].Year}</i>";
        }

        await botClient.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: TitleOfFilms,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }
}