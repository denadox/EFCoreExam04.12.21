namespace Theatre.DataProcessor
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Text;
    using Theatre.Data;
    using Theatre.Data.Models;
    using Theatre.Data.Models.Enums;
    using Theatre.DataProcessor.ImportDto;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";

        private const string SuccessfulImportPlay
            = "Successfully imported {0} with genre {1} and a rating of {2}!";

        private const string SuccessfulImportActor
            = "Successfully imported actor {0} as a {1} character!";

        private const string SuccessfulImportTheatre
            = "Successfully imported theatre {0} with #{1} tickets!";

        public static string ImportPlays(TheatreContext context, string xmlString)
        {
            var sb = new StringBuilder();

            var plays = new List<Play>();

            var playsDtos = XmlConverter.Deserializer<ImportPlaysDto>(xmlString, "Plays");

            foreach (var playDto in playsDtos)
            {
                var duration = TimeSpan.ParseExact(playDto.Duration, "c", CultureInfo.InvariantCulture);

                if (!IsValid(playDto) || duration.Hours < 1)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }
                                        

                if (!Enum.TryParse(typeof(Genre), playDto.Genre, out var genre))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }
                var play = new Play()
                {
                    Title = playDto.Title,
                    Duration = duration,
                    Rating = playDto.Rating,
                    Genre = (Genre)genre,
                    Description = playDto.Description,
                    Screenwriter = playDto.Screenwriter
                };

                plays.Add(play);
                sb.AppendLine($"Successfully imported {play.Title} with genre {play.Genre} and a rating of {play.Rating}!");
            }
            context.AddRange(plays);
            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }

        public static string ImportCasts(TheatreContext context, string xmlString)
        {
            var sb = new StringBuilder();

            List<Cast> casts = new List<Cast>();

            var castsDtos = XmlConverter.Deserializer<ImportCastsDto>(xmlString, "Casts");

            foreach (var castDto in castsDtos)
            {
                if (!IsValid(castDto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var cast = new Cast()
                {
                    FullName = castDto.FullName,
                    IsMainCharacter = castDto.IsMainCharacter,
                    PhoneNumber = castDto.PhoneNumber,
                    PlayId = castDto.PlayId
                };

                if (castDto.IsMainCharacter == true)
                {
                    casts.Add(cast);
                    sb.AppendLine($"Successfully imported actor {cast.FullName} as a main character!");
                }

                else if (castDto.IsMainCharacter == false)
                {
                    casts.Add(cast);
                    sb.AppendLine($"Successfully imported actor {cast.FullName} as a lesser character!");
                }
            }

            context.AddRange(casts);
            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }

        public static string ImportTtheatersTickets(TheatreContext context, string jsonString)
        {
            var sb = new StringBuilder();

            var theatres = new List<Theatre>();

            var allTheatres = JsonConvert.DeserializeObject<IEnumerable<ImportTheatreDto>>(jsonString);

            foreach (var theatre in allTheatres)
            {
                if (!IsValid(theatre))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var validTheatre = new Theatre()
                {
                    Name = theatre.Name,
                    NumberOfHalls = theatre.NumberOfHalls,
                    Director = theatre.Director
                };

                foreach (var ticket in theatre.Tickets)
                {
                    if (IsValid(ticket))
                    {
                        validTheatre.Tickets.Add(new Ticket()
                        {
                            Price = ticket.Price,
                            RowNumber = ticket.RowNumber,
                            PlayId = ticket.PlayId
                        });
                    }
                    else
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }
                }
                theatres.Add(validTheatre);
                sb.AppendLine($"Successfully imported theatre {theatre.Name} with #{validTheatre.Tickets.Count} tickets!");
            }

            context.Theatres.AddRange(theatres);
            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }


        private static bool IsValid(object obj)
        {
            var validator = new ValidationContext(obj);
            var validationRes = new List<ValidationResult>();

            var result = Validator.TryValidateObject(obj, validator, validationRes, true);
            return result;
        }
    }
}
