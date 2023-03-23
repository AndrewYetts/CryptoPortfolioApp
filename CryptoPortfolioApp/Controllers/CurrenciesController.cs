using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CryptoPortfolioApp.Data;
using CryptoPortfolioApp.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace CryptoPortfolioApp.Controllers
{
    [Authorize]
    public class CurrenciesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CurrenciesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static async Task<HttpResponseMessage> GetCurrencyUpdates(string currencyNames)
        {
            var client = new HttpClient() { BaseAddress = new Uri("https://api.coingecko.com/api/v3/simple/price") };
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.GetAsync($"?ids={currencyNames}&vs_currencies=usd&include_24hr_change=true");
            response.EnsureSuccessStatusCode();
            return response;
        }

        // GET: Currencies
        public async Task<IActionResult> Index(string searchString)
        {
            // Get the currency names that the user owns in csv format
            var ownerName = User.Identity != null && User.Identity.Name != null ? User.Identity.Name : string.Empty;
            var ownedCurrencies = _context.Currencies.Where(c => c.OwnerName == ownerName).ToList();

            if (!string.IsNullOrEmpty(searchString))
            {
                ownedCurrencies = ownedCurrencies.Where(c => c.Name.ToLower() == searchString.ToLower()).ToList();
            }

            var currencyNames = string.Join(",", ownedCurrencies.Select(c => c.Name));

            // Get updated currency data from CoinGecko API
            HttpResponseMessage response = GetCurrencyUpdates(currencyNames).Result;

            // Update all owned currencies with the HTTP response
            var updatedCurrency = JObject.Parse(await response.Content.ReadAsStringAsync());

            foreach (Currency c in ownedCurrencies)
            {
                if (updatedCurrency != null)
                {
                    var currency = updatedCurrency[c.Name.ToLower()] ?? string.Empty;
                    c.Value = (decimal) (currency["usd"] ?? decimal.Zero);
                    c.DailyChange = Math.Round((decimal) (currency["usd_24h_change"] ?? decimal.Zero), 2);
                    c.PortfolioValue = c.Value * c.Quantity;
                }
            }

            _context.UpdateRange(ownedCurrencies);
            await _context.SaveChangesAsync();
            return View(ownedCurrencies);
        }

        // GET: Currencies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Currencies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,OwnerName,Value,Quantity,PortfolioValue,DailyChange")] Currency currency)
        {
            if (User.Identity != null && User.Identity.Name != null)
            {
                currency.OwnerName = User.Identity.Name;
            }

            if (ModelState.IsValid)
            {
                _context.Add(currency);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(currency);
        }

        // GET: Currencies/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.Currencies == null)
            {
                return NotFound();
            }

            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null)
            {
                return NotFound();
            }
            return View(currency);
        }

        // POST: Currencies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,OwnerName,Value,Quantity,PortfolioValue,DailyChange")] Currency currency)
        {
            if (id != currency.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(currency);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CurrencyExists(currency.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(currency);
        }

        // GET: Currencies/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.Currencies == null)
            {
                return NotFound();
            }

            var currency = await _context.Currencies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (currency == null)
            {
                return NotFound();
            }

            return View(currency);
        }

        // POST: Currencies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.Currencies == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Currencies'  is null.");
            }
            var currency = await _context.Currencies.FindAsync(id);
            if (currency != null)
            {
                _context.Currencies.Remove(currency);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CurrencyExists(string id)
        {
          return (_context.Currencies?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
