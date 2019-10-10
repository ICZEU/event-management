﻿using EventManagement.Infrastructure.Data;
using EventManagement.TicketGeneration;
using EventManagement.WebApp.Shared.Mvc;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EventManagement.WebApp.Controllers
{
    /// <summary>
    /// Controller to download tickets with an internet browser.
    /// </summary>
    [SecurityHeaders]
    [Authorize(EventManagementConstants.AdminApi.PolicyName)]
    public class TicketDownloadController : Controller
    {
        private readonly EventsDbContext _context;

        public TicketDownloadController(EventsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Download a ticket as pdf.
        /// </summary>
        /// <param name="id">Id of the ticket.</param>
        /// <returns>pdf file</returns>
        [HttpGet("tickets/{id}/pdf")]
        public IActionResult DownloadAsPdf(Guid id)
        {
            var ticket = _context.Tickets
                .Include(x => x.Event)
                .Include(x => x.TicketType)
                .SingleOrDefault(x => x.Id == id);

            if (ticket == null)
                return NotFound();

            TicketData values = Map(ticket);

            var stream = new MemoryStream();
            var generator = new PdfTicketGenerator();
            generator.GenerateTicket(values, stream);
            stream.Position = 0;

            string fileDownloadName = ticket.TicketNumber + ".pdf";
            Response.Headers.Add("Content-Disposition", $"inline; filename={fileDownloadName}");
            return File(stream, "application/pdf");
        }

        private TicketData Map(ApplicationCore.Models.Ticket ticket)
        {
            var values = new TicketData
            {
                EventName = ticket.Event.Name,
                TicketId = ticket.TicketNumber,
                QrValue = GetTicketValidationUrl(ticket),
                EventLogo = $"{BaseUrl}/One_Events_2019.png",
                Host = ticket.Event.Host,
                EventDate = ticket.Event.StartTime.ToString("dddd, dd.MM.yyyy"),
                EventLocation = ticket.Event.Location,
                TicketType = ticket.TicketType.Name,
                Price = $"{ticket.TicketType.Price} € (inkl. Vorverkaufsgebühr)",
                BeginTime = ticket.Event.StartTime.ToString("hh:mm") + " Uhr",
                Address = GetAddressRows(ticket).ToList(),
                BookingDate = ticket.CreatedAt.ToString("dd.MM.yyyy"),
                BookingNumber = ticket.TicketNumber
            };
            if (ticket.LastName != null)
            {
                values.Buyer = $"{ticket.FirstName} {ticket.LastName}".TrimStart();
            }
            if (ticket.Event.EntranceTime != null)
            {
                values.EntranceTime =
                    ticket.Event.EntranceTime.Value.ToString("hh:mm") + " Uhr";
            }
            return values;
        }

        private static IEnumerable<string> GetAddressRows(ApplicationCore.Models.Ticket ticket)
        {
            foreach (string row in ticket.Event.Address.Split("\n"))
            {
                string s = row.Trim();
                if (s.Length > 0)
                    yield return s;
            }

            yield return $"{ticket.Event.ZipCode} {ticket.Event.City}";
        }

        private string GetTicketValidationUrl(ApplicationCore.Models.Ticket ticket)
        {
            return Url.ActionAbsoluteUrl<TicketValidationController>(
                nameof(TicketValidationController.ValidateTicketByQrCodeValueAsync),
                new { secret = ticket.TicketSecret });
        }

        private string BaseUrl
        {
            get
            {
                var request = Request;
                var host = request.Host.ToUriComponent();
                var pathBase = request.PathBase.ToUriComponent();
                return $"{request.Scheme}://{host}{pathBase}";
            }
        }
    }
}