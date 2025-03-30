﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WishlistBot.Model;

#nullable disable

namespace WishlistBot.Migrations
{
    [DbContext(typeof(UserContext))]
    [Migration("20250330202931_Adjustments")]
    partial class Adjustments
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.3");

            modelBuilder.Entity("WishlistBot.Model.BroadcastModel", b =>
                {
                    b.Property<int>("BroadcastId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("DateTimeSent")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Deleted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("BroadcastId");

                    b.ToTable("Broadcasts");
                });

            modelBuilder.Entity("WishlistBot.Model.LinkModel", b =>
                {
                    b.Property<int>("LinkId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("WishDraftModelWishDraftId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WishId")
                        .HasColumnType("INTEGER");

                    b.HasKey("LinkId");

                    b.HasIndex("WishDraftModelWishDraftId");

                    b.HasIndex("WishId");

                    b.ToTable("Links");
                });

            modelBuilder.Entity("WishlistBot.Model.ReceivedBroadcastModel", b =>
                {
                    b.Property<int>("ReceivedBroadcastId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("BroadcastId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MessageId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ReceiverId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ReceivedBroadcastId");

                    b.HasIndex("BroadcastId");

                    b.HasIndex("ReceiverId");

                    b.ToTable("ReceivedBroadcasts");
                });

            modelBuilder.Entity("WishlistBot.Model.SettingsModel", b =>
                {
                    b.Property<int>("SettingsId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ReceiveNotifications")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("SendNotifications")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("SettingsId");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("WishlistBot.Model.SubscriptionModel", b =>
                {
                    b.Property<int>("SubscriptionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("SubscriberId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TargetId")
                        .HasColumnType("INTEGER");

                    b.HasKey("SubscriptionId");

                    b.HasIndex("TargetId");

                    b.HasIndex("SubscriberId", "TargetId")
                        .IsUnique();

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("WishlistBot.Model.UserModel", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AllowedQueries")
                        .HasColumnType("TEXT");

                    b.Property<int>("BotState")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("LastBotMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LastQueryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("QueryParams")
                        .HasColumnType("TEXT");

                    b.Property<string>("SubscribeId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Tag")
                        .HasColumnType("TEXT");

                    b.Property<long>("TelegramId")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserId");

                    b.HasIndex("TelegramId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("WishlistBot.Model.WishDraftModel", b =>
                {
                    b.Property<int>("WishDraftId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ClaimerId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("OriginalId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OwnerId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PriceRange")
                        .HasColumnType("INTEGER");

                    b.HasKey("WishDraftId");

                    b.HasIndex("ClaimerId");

                    b.HasIndex("OriginalId");

                    b.HasIndex("OwnerId")
                        .IsUnique();

                    b.ToTable("WishDrafts");
                });

            modelBuilder.Entity("WishlistBot.Model.WishModel", b =>
                {
                    b.Property<int>("WishId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ClaimerId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("OwnerId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PriceRange")
                        .HasColumnType("INTEGER");

                    b.HasKey("WishId");

                    b.HasIndex("ClaimerId");

                    b.HasIndex("OwnerId");

                    b.ToTable("Wishes");
                });

            modelBuilder.Entity("WishlistBot.Model.LinkModel", b =>
                {
                    b.HasOne("WishlistBot.Model.WishDraftModel", null)
                        .WithMany("Links")
                        .HasForeignKey("WishDraftModelWishDraftId");

                    b.HasOne("WishlistBot.Model.WishModel", "Wish")
                        .WithMany("Links")
                        .HasForeignKey("WishId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Wish");
                });

            modelBuilder.Entity("WishlistBot.Model.ReceivedBroadcastModel", b =>
                {
                    b.HasOne("WishlistBot.Model.BroadcastModel", "Broadcast")
                        .WithMany()
                        .HasForeignKey("BroadcastId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("WishlistBot.Model.UserModel", "Receiver")
                        .WithMany("ReceivedBroadcasts")
                        .HasForeignKey("ReceiverId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Broadcast");

                    b.Navigation("Receiver");
                });

            modelBuilder.Entity("WishlistBot.Model.SettingsModel", b =>
                {
                    b.HasOne("WishlistBot.Model.UserModel", "User")
                        .WithOne("Settings")
                        .HasForeignKey("WishlistBot.Model.SettingsModel", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("WishlistBot.Model.SubscriptionModel", b =>
                {
                    b.HasOne("WishlistBot.Model.UserModel", "Subscriber")
                        .WithMany("Subscriptions")
                        .HasForeignKey("SubscriberId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("WishlistBot.Model.UserModel", "Target")
                        .WithMany("Subscribers")
                        .HasForeignKey("TargetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Subscriber");

                    b.Navigation("Target");
                });

            modelBuilder.Entity("WishlistBot.Model.WishDraftModel", b =>
                {
                    b.HasOne("WishlistBot.Model.UserModel", "Claimer")
                        .WithMany()
                        .HasForeignKey("ClaimerId");

                    b.HasOne("WishlistBot.Model.WishModel", "Original")
                        .WithMany()
                        .HasForeignKey("OriginalId");

                    b.HasOne("WishlistBot.Model.UserModel", "Owner")
                        .WithOne("CurrentWish")
                        .HasForeignKey("WishlistBot.Model.WishDraftModel", "OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Claimer");

                    b.Navigation("Original");

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("WishlistBot.Model.WishModel", b =>
                {
                    b.HasOne("WishlistBot.Model.UserModel", "Claimer")
                        .WithMany("ClaimedWishes")
                        .HasForeignKey("ClaimerId");

                    b.HasOne("WishlistBot.Model.UserModel", "Owner")
                        .WithMany("Wishes")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Claimer");

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("WishlistBot.Model.UserModel", b =>
                {
                    b.Navigation("ClaimedWishes");

                    b.Navigation("CurrentWish");

                    b.Navigation("ReceivedBroadcasts");

                    b.Navigation("Settings")
                        .IsRequired();

                    b.Navigation("Subscribers");

                    b.Navigation("Subscriptions");

                    b.Navigation("Wishes");
                });

            modelBuilder.Entity("WishlistBot.Model.WishDraftModel", b =>
                {
                    b.Navigation("Links");
                });

            modelBuilder.Entity("WishlistBot.Model.WishModel", b =>
                {
                    b.Navigation("Links");
                });
#pragma warning restore 612, 618
        }
    }
}
