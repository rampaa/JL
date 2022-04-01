﻿using System;
using System.Text.Json;
using JL.Core.Deconjugation;
using NUnit.Framework;

namespace JL.Core.Tests.Deconjugation
{
    [TestFixture]
    public class DeconjugatorTests
    {
        [Test]
        public void Deconjugate_わからない()
        {
            // Arrange
            string expected =
                "[{\"Text\":\"わからない\",\"OriginalText\":\"わからない\",\"Tags\":[],\"Seentext\":[],\"Process\":[]},{\"Text\":\"わからなる\",\"OriginalText\":\"わからない\",\"Tags\":[\"uninflectable\",\"v5aru\"],\"Seentext\":[\"わからない\",\"わからなる\"],\"Process\":[\"imperative\"]},{\"Text\":\"わからなう\",\"OriginalText\":\"わからない\",\"Tags\":[\"stem-ren\",\"v5u\"],\"Seentext\":[\"わからない\",\"わからなう\"],\"Process\":[\"(infinitive)\"]},{\"Text\":\"わからなう\",\"OriginalText\":\"わからない\",\"Tags\":[\"stem-ren\",\"v5u-s\"],\"Seentext\":[\"わからない\",\"わからなう\"],\"Process\":[\"(infinitive)\"]},{\"Text\":\"わからないる\",\"OriginalText\":\"わからない\",\"Tags\":[\"stem-ren\",\"v1\"],\"Seentext\":[\"わからない\",\"わからないる\"],\"Process\":[\"(infinitive)\"]},{\"Text\":\"わからなる\",\"OriginalText\":\"わからない\",\"Tags\":[\"stem-ren\",\"v5aru\"],\"Seentext\":[\"わからない\",\"わからなる\"],\"Process\":[\"(infinitive)\"]},{\"Text\":\"わからないい\",\"OriginalText\":\"わからない\",\"Tags\":[\"stem-adj-base\",\"adj-i\"],\"Seentext\":[\"わからない\",\"わからないい\"],\"Process\":[\"(stem)\"]},{\"Text\":\"わから\",\"OriginalText\":\"わからない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\"],\"Seentext\":[\"わからない\",\"わから\"],\"Process\":[\"negative\"]},{\"Text\":\"わから\",\"OriginalText\":\"わからない\",\"Tags\":[\"adj-i\",\"stem-ku\"],\"Seentext\":[\"わからない\",\"わから\"],\"Process\":[\"negative\"]},{\"Text\":\"わからない\",\"OriginalText\":\"わからない\",\"Tags\":[\"stem-ren\",\"v1\",\"stem-izenkei\"],\"Seentext\":[\"わからない\",\"わからないる\"],\"Process\":[\"(infinitive)\",\"potential\"]},{\"Text\":\"わからな\",\"OriginalText\":\"わからない\",\"Tags\":[\"stem-ren\",\"v1\",\"stem-te\"],\"Seentext\":[\"わからない\",\"わからないる\",\"わからな\"],\"Process\":[\"(infinitive)\",\"teiru\"]},{\"Text\":\"わからない\",\"OriginalText\":\"わからない\",\"Tags\":[\"stem-ren\",\"v1\",\"stem-te\"],\"Seentext\":[\"わからない\",\"わからないる\"],\"Process\":[\"(infinitive)\",\"teru (teiru)\"]},{\"Text\":\"わから\",\"OriginalText\":\"わからない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"stem-a\"],\"Seentext\":[\"わからない\",\"わから\"],\"Process\":[\"negative\",\"(mizenkei)\"]},{\"Text\":\"わからる\",\"OriginalText\":\"わからない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"v1\"],\"Seentext\":[\"わからない\",\"わから\",\"わからる\"],\"Process\":[\"negative\",\"(mizenkei)\"]},{\"Text\":\"わかる\",\"OriginalText\":\"わからない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"v5aru\"],\"Seentext\":[\"わからない\",\"わから\",\"わかる\"],\"Process\":[\"negative\",\"(mizenkei)\"]},{\"Text\":\"わかる\",\"OriginalText\":\"わからない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"stem-a\",\"v5r\"],\"Seentext\":[\"わからない\",\"わから\",\"わかる\"],\"Process\":[\"negative\",\"(mizenkei)\",\"('a' stem)\"]},{\"Text\":\"わから\",\"OriginalText\":\"わからない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-izenkei\"],\"Seentext\":[\"わからない\",\"わから\",\"わからる\"],\"Process\":[\"negative\",\"(mizenkei)\",\"potential\"]},{\"Text\":\"わから\",\"OriginalText\":\"わからない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-te\"],\"Seentext\":[\"わからない\",\"わから\",\"わからる\"],\"Process\":[\"negative\",\"(mizenkei)\",\"teru (teiru)\"]}]";

            // Act
            System.Collections.Generic.HashSet<Form> result = Deconjugator.Deconjugate("わからない");
            string actual = JsonSerializer.Serialize(result, Storage.JsoUnsafeEscaping);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Deconjugate_このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない()
        {
            // Arrange
            string expected =
                "[{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[],\"Seentext\":[],\"Process\":[]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくなる\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"uninflectable\",\"v5aru\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくなる\"],\"Process\":[\"imperative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくなう\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"stem-ren\",\"v5u\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくなう\"],\"Process\":[\"(infinitive)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくなう\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"stem-ren\",\"v5u-s\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくなう\"],\"Process\":[\"(infinitive)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくないる\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"stem-ren\",\"v1\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくないる\"],\"Process\":[\"(infinitive)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくなる\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"stem-ren\",\"v5aru\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくなる\"],\"Process\":[\"(infinitive)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくないい\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"stem-adj-base\",\"adj-i\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくないい\"],\"Process\":[\"(stem)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\"],\"Process\":[\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\"],\"Process\":[\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"stem-ren\",\"v1\",\"stem-izenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくないる\"],\"Process\":[\"(infinitive)\",\"potential\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくな\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"stem-ren\",\"v1\",\"stem-te\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくないる\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくな\"],\"Process\":[\"(infinitive)\",\"teiru\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"stem-ren\",\"v1\",\"stem-te\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくないる\"],\"Process\":[\"(infinitive)\",\"teru (teiru)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"stem-a\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\"],\"Process\":[\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくる\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"v1\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくる\"],\"Process\":[\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\"],\"Process\":[\"negative\",\"(adverbial stem)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-izenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくる\"],\"Process\":[\"negative\",\"(mizenkei)\",\"potential\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-te\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくる\"],\"Process\":[\"negative\",\"(mizenkei)\",\"teru (teiru)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"stem-a\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくる\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-izenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\",\"potential\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-te\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\",\"teru (teiru)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"stem-a\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくる\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-izenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\",\"potential\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-te\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\",\"teru (teiru)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"stem-a\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくる\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃない\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃない\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-izenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\",\"potential\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-te\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\",\"teru (teiru)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃ\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃない\",\"このスレってよくなくなくなくなくなくなくなくないじゃ\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃ\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃない\",\"このスレってよくなくなくなくなくなくなくなくないじゃ\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃ\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"stem-a\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃない\",\"このスレってよくなくなくなくなくなくなくなくないじゃ\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃる\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃない\",\"このスレってよくなくなくなくなくなくなくなくないじゃ\",\"このスレってよくなくなくなくなくなくなくなくないじゃる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃ\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-izenkei\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃない\",\"このスレってよくなくなくなくなくなくなくなくないじゃ\",\"このスレってよくなくなくなくなくなくなくなくないじゃる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\",\"potential\"]},{\"Text\":\"このスレってよくなくなくなくなくなくなくなくないじゃ\",\"OriginalText\":\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"Tags\":[\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-ku\",\"adj-i\",\"stem-mizenkei\",\"v1\",\"stem-te\"],\"Seentext\":[\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃなくない\",\"このスレってよくなくなくなくなくなくなくなくないじゃなく\",\"このスレってよくなくなくなくなくなくなくなくないじゃない\",\"このスレってよくなくなくなくなくなくなくなくないじゃ\",\"このスレってよくなくなくなくなくなくなくなくないじゃる\"],\"Process\":[\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(adverbial stem)\",\"negative\",\"(mizenkei)\",\"teru (teiru)\"]}]";

            // Act
            System.Collections.Generic.HashSet<Form> result = Deconjugator.Deconjugate("このスレってよくなくなくなくなくなくなくなくないじゃなくなくなくなくない");
            string actual = JsonSerializer.Serialize(result, Storage.JsoUnsafeEscaping);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Deconjugate_MemoryUsageIsAcceptable100()
        {
            // Arrange
            int iterations = 100;
            double expected = 75000000 + 800000;

            // Act
            double start = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                Deconjugator.Deconjugate("飽きて", false);
                Deconjugator.Deconjugate("座り込む", false);
                Deconjugator.Deconjugate("していられない", false);
                Deconjugator.Deconjugate("なく", false);
                Deconjugator.Deconjugate("握って", false);
                Deconjugator.Deconjugate("開き", false);
                Deconjugator.Deconjugate("伸ばして", false);
                Deconjugator.Deconjugate("戻す", false);
            }

            long end = GC.GetAllocatedBytesForCurrentThread();
            double actual = end - start;

            // Assert
            Assert.Less(actual, expected);
        }

        [Test]
        public void Deconjugate_MemoryUsageIsAcceptable1000()
        {
            // Arrange
            int iterations = 1000;
            double expected = 750000000 + 8000000;

            // Act
            double start = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                Deconjugator.Deconjugate("飽きて", false);
                Deconjugator.Deconjugate("座り込む", false);
                Deconjugator.Deconjugate("していられない", false);
                Deconjugator.Deconjugate("なく", false);
                Deconjugator.Deconjugate("握って", false);
                Deconjugator.Deconjugate("開き", false);
                Deconjugator.Deconjugate("伸ばして", false);
                Deconjugator.Deconjugate("戻す", false);
            }

            long end = GC.GetAllocatedBytesForCurrentThread();
            double actual = end - start;

            // Assert
            Assert.Less(actual, expected);
        }

        [Test, Explicit]
        public void Deconjugate_MemoryUsageIsAcceptable10000()
        {
            // Arrange
            int iterations = 10000;
            double expected = 7500000000 + 80000000;

            // Act
            double start = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                Deconjugator.Deconjugate("飽きて", false);
                Deconjugator.Deconjugate("座り込む", false);
                Deconjugator.Deconjugate("していられない", false);
                Deconjugator.Deconjugate("なく", false);
                Deconjugator.Deconjugate("握って", false);
                Deconjugator.Deconjugate("開き", false);
                Deconjugator.Deconjugate("伸ばして", false);
                Deconjugator.Deconjugate("戻す", false);
            }

            long end = GC.GetAllocatedBytesForCurrentThread();
            double actual = end - start;

            // Assert
            Assert.Less(actual, expected);
        }
    }
}
