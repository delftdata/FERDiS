using BlackSP.Benchmarks.NEXMark.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace BlackSP.Benchmarks.NEXMark.Generator
{
    public class XMLParser
    {

        private XDocument XDoc { get; }

        public XMLParser(string xmlString)
        {
            _ = xmlString ?? throw new ArgumentNullException(nameof(xmlString));
            XDoc = XDocument.Parse(xmlString); 
        }

        public XMLParser(XDocument xDoc)
        {
            XDoc = xDoc ?? throw new ArgumentNullException(nameof(xDoc));
        }

        /// <summary>
        /// Extracts all person elements from the provided xml file
        /// as Person model class instances
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Person> GetPeople()
        {
            foreach (var person in XDoc.Descendants("person"))
            {
                var addressElem = person.Element("address");
                Address address = null;
                if(addressElem != null)
                {
                    address = new Address
                    {
                        Street = addressElem.Element("street")?.Value ?? throw new InvalidDataException("Missing street element on address"),
                        City = addressElem.Element("city")?.Value ?? throw new InvalidDataException("Missing street city on address"),
                        Country = addressElem.Element("country")?.Value ?? throw new InvalidDataException("Missing country element on address"),
                        Province = addressElem.Element("province")?.Value ?? throw new InvalidDataException("Missing province element on address"),
                        Zipcode = addressElem.Element("zipcode")?.Value ?? throw new InvalidDataException("Missing zipcode element on address")
                    };
                }

                var profileElem = person.Element("profile");
                Profile profile = null;
                if(profileElem != null)
                {
                    profile = new Profile
                    {
                        Interests = profileElem.Elements("interest").Select(interest => int.Parse(interest.Attribute("category").Value)),
                        Income = double.Parse(profileElem.Attribute("income")?.Value ?? throw new InvalidDataException("Missing income attribute on profile")),
                        IsBusiness = profileElem.Element("business").Value == "Yes" ? true : false,
                        Education = profileElem.Element("education")?.Value ?? null,
                        Age = int.Parse(profileElem.Element("age")?.Value ?? "-1"),
                        Gender = profileElem.Element("gender")?.Value ?? null
                    };
                }
                yield return new Person
                {
                    Id = int.Parse(person.Attribute("id")?.Value ?? throw new InvalidDataException("Missing id attribute on person")),
                    FullName = person.Element("name")?.Value ?? throw new InvalidDataException("Missing name element on person"),
                    Email = person.Element("emailaddress")?.Value ?? throw new InvalidDataException("Missing emailaddress element on person"),
                    CreditCard = person.Element("creditcard")?.Value ?? null, //explicitly allowing null
                    Website = person.Element("homepage")?.Value ?? null, //explicitly allowing null
                    Address = address,
                    Profile = profile
                };
            }
        }

        public IEnumerable<Auction> GetAuctions()
        {
            foreach (var auction in XDoc.Descendants("open_auction"))
            {
                var auctionId = int.Parse(auction.Attribute("id")?.Value ?? throw new InvalidDataException("Missing id attribute on open_auction"));
                if (auction.Element("bidder") != null) //not an auction, skip
                {
                    continue;
                }
                
                var itemId = int.Parse(auction.Element("itemref")?.Attribute("item")?.Value ?? throw new InvalidDataException("Missing itemId on open_auction"));
                var personId = int.Parse(auction.Element("seller")?.Attribute("person")?.Value ?? throw new InvalidDataException("Missing personId on open_auction"));
                var category = int.Parse(auction.Element("category")?.Value ?? throw new InvalidDataException("Missing category element on open_auction"));
                var quantity = int.Parse(auction.Element("quantity")?.Value ?? throw new InvalidDataException("Missing quantity element on open_auction"));
                var startTime = int.Parse(auction.Descendants("start").FirstOrDefault()?.Value ?? throw new InvalidDataException("Missing start element on open_auction"));
                var endTime = int.Parse(auction.Descendants("end").FirstOrDefault()?.Value ?? throw new InvalidDataException("Missing end element on open_auction"));                

                yield return new Auction
                {
                    Id = auctionId,
                    ItemId = itemId,
                    PersonId = personId,
                    CategoryId = category,
                    Quantity = quantity,
                    StartTime = startTime,
                    EndTime = endTime
                };
            }
        }

        public IEnumerable<Bid> GetBids()
        {
            foreach (var auction in XDoc.Descendants("open_auction"))
            {
                var auctionId = int.Parse(auction.Attribute("id")?.Value ?? throw new InvalidDataException("Missing id attribute on open_auction"));
                var bidder = auction.Element("bidder");
                if (bidder == null) //not a bid, skip
                {
                    continue;    
                }
                
                var time = int.Parse(bidder.Element("time")?.Value ?? throw new InvalidDataException("Missing time element on bidder"));
                var personId = int.Parse(bidder.Element("person_ref").Attribute("person")?.Value ?? throw new InvalidDataException("Missing person_ref on bidder"));
                var bid = double.Parse(bidder.Element("bid")?.Value ?? throw new InvalidDataException("Missing bid element on bidder"));
                
                yield return new Bid
                {
                    AuctionId = auctionId,
                    PersonId = personId,
                    Time = time,
                    Amount = bid
                };
            }
        }

    }
}
