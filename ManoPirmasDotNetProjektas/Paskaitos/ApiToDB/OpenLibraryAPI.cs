﻿using ManoPirmasDotNetProjektas.Paskaitos.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ManoPirmasDotNetProjektas.Paskaitos.ApiToDB
{
    public class OpenLibraryAPI : IOpenLibraryAPI
    {
        private readonly string _booksEndpoint = "https://openlibrary.org/books/";
        private readonly string _baseEndpoint = "https://openlibrary.org";

        private readonly int _bookLimit = 38247449;

        private readonly ILoggerServise _logger;

        public OpenLibraryAPI(ILoggerServise logger)
        {
            _logger = logger;
        }

        public async Task<BookDto> GetBookFromUrl(string url)
        {
            try
            {
                using var client = new HttpClient();
                var result = await client.GetAsync(url);
                var body = await result.Content.ReadAsStringAsync();

                try
                {
                    var book = JsonConvert.DeserializeObject<BookDto>(body);

                    if (string.IsNullOrEmpty(book.error))
                    {
                        return book;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    await _logger.LogError("cannot deserialize responce to BookDto");
                    await _logger.LogError($"Body of response: {body}");
                    await _logger.LogError(ex.Message);
                    await _logger.LogError(ex.StackTrace);
                }
            }
            catch (WebException ex)
            {
                await _logger.LogError($"Failed to reach {url}, status: ({ex.Status})");
                await _logger.LogError(ex.Message);
                await _logger.LogError(ex.StackTrace);
            }
            catch (Exception ex)
            {
                await _logger.LogError($"Failed to reach {url}");
                await _logger.LogError(ex.Message);
                await _logger.LogError(ex.StackTrace);
            }

            return null;
        }

        public string[] GenerateRandomBookUrl(int quantity)
        {
            var random = new Random();

            var bookIds = new List<int>();

            while (bookIds.Count() < quantity)
            {
                int id = Convert.ToInt32(random.NextDouble() * (_bookLimit - 1) + 1);

                if (!bookIds.Contains(id))
                {
                    bookIds.Add(id);
                }
            }

            var booksUrl = new string[quantity];

            for (int i = 0; i < quantity; i++)
            {
                booksUrl[i] = $"{_booksEndpoint}OL{bookIds[i]}M.json";
            }

            return booksUrl;
        }

        public async Task AddAuthorToBook(BookDto book)
        {
            book.AuthorDto = new List<AuthorDto>();

            if (book?.authors is not null)
            {
                foreach (var author in book.authors)
                {
                    var url = $"{_baseEndpoint}{author.key}.json";

                    try
                    {
                        using var client = new HttpClient();
                        var result = await client.GetAsync(url);
                        var body = await result.Content.ReadAsStringAsync();

                        try
                        {
                            book.AuthorDto.Add(JsonConvert.DeserializeObject<AuthorDto>(body));
                        }
                        catch (Exception ex)
                        {
                            await _logger.LogError("cannot deserialize responce to AuthorDto class");
                            await _logger.LogError($"Body of response: {body}");
                            await _logger.LogError(ex.Message);
                            await _logger.LogError(ex.StackTrace);
                        }
                    }
                    catch (WebException ex)
                    {
                        await _logger.LogError($"Failed to reach {url}, status: ({ex.Status})");
                        await _logger.LogError(ex.Message);
                        await _logger.LogError(ex.StackTrace);
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogError($"Failed to reach {url}");
                        await _logger.LogError(ex.Message);
                        await _logger.LogError(ex.StackTrace);
                    }
                }
            }
        }
    }
}
