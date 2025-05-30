using Emma.Data.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Emma.Api.Controllers
{
    /// <summary>
    /// API endpoints for managing Contacts in the Emma AI Platform.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ContactsController : ControllerBase
    {
        /// <summary>
        /// Gets all contacts.
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<Contact>> GetAll()
        {
            // TODO: Replace with real data source
            return Ok(new List<Contact>());
        }

        /// <summary>
        /// Gets a specific contact by ID.
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<Contact> GetById(Guid id)
        {
            // TODO: Replace with real data source
            return Ok(new Contact { Id = id });
        }

        /// <summary>
        /// Creates a new contact.
        /// </summary>
        [HttpPost]
        public ActionResult<Contact> Create([FromBody] Contact contact)
        {
            // TODO: Replace with real creation logic
            contact.Id = Guid.NewGuid();
            return CreatedAtAction(nameof(GetById), new { id = contact.Id }, contact);
        }
    }
}
