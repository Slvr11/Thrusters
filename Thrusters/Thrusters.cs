using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfinityScript;

namespace Thrusters
{
    public class Thrusters : BaseScript
    {
        public Thrusters()
            : base()
        {
            GSCFunctions.PreCacheShader("line_horizontal");

            GSCFunctions.SetDevDvarIfUninitialized("g_thrusterType", 0);

            PlayerConnected += onPlayerConnect;
        }

        private static void onPlayerConnect(Entity player)
        {
            initThrusterBehavior(player);

            player.SpawnedPlayer += () => onPlayerSpawned(player);

            if (GSCFunctions.GetDvar("g_gametype") == "war") scaleTDMScore();

            if (GSCFunctions.GetDvar("g_thrusterType") == "0")
            {
                if (GSCFunctions.GetDvar("g_gametype") == "oic")
                {
                    player.TakeAllWeapons();
                    player.IPrintLnBold("Use your ^3Exo-Slam ^7(^3Exo Jump ^7+ ^3[{+stance}]^7) to kill enemy players!");
                }
            }
        }

        private static void onPlayerSpawned(Entity player)
        {
            player.SetField("thrustersActive", 0);
            if (GSCFunctions.GetDvar("g_thrusterType") == "1")
            {
                player.SetField("thrusterEnergy", 25);
                updateThrusterBar(player, 25);
            }
            else
            {
                player.SetField("thrusterEnergy", 3);
                if (GSCFunctions.GetDvar("g_gametype") == "oic")
                {
                    player.TakeAllWeapons();
                    player.IPrintLnBold("Use your ^3Exo-Slam ^7(^3Exo Jump ^7+ ^3[{+stance}]^7) to kill enemy players!");
                }
            }
            player.SetField("lastThrust", 0);
            //player.SetPerk("specialty_jumpdive", true, false);

            //OnInterval(50, () => checkForWallRun(player));
        }

        public override void OnPlayerDamage(Entity player, Entity inflictor, Entity attacker, int damage, int dFlags, string mod, string weapon, Vector3 point, Vector3 dir, string hitLoc)
        {
            if (mod == "MOD_FALLING")
            {
                //player.Health += 99;
                int prevHealth = player.Health;
                player.Health = 175;
                //if (player.Health > 100) player.Health = 100;
                AfterDelay(50, () =>
                {
                    if (player.Health > 100) player.Health = prevHealth;
                });
                //Log.Debug("Health is at {0}; prevHealth {2}; damage taken {1}", player.Health, damage, prevHealth);
            }
        }
        public override void OnPlayerDisconnect(Entity player)
        {
            AfterDelay(50, scaleTDMScore);
        }

        private static void scaleTDMScore()
        {
            if (GSCFunctions.GetDvar("g_gametype") != "war") return;

            int totalPlayers = Players.Count;
            int highestScore = Math.Max(GSCFunctions.GetTeamScore("allies"), GSCFunctions.GetTeamScore("axis"));
            switch (totalPlayers)
            {
                case 1:
                case 2:
                case 3:
                    if (highestScore < 1500) GSCFunctions.SetDynamicDvar("scr_war_scorelimit", 1500);
                    break;
                case 4:
                case 5:
                    if (highestScore < 2500) GSCFunctions.SetDynamicDvar("scr_war_scorelimit", 2500);
                    break;
                case 6:
                case 7:
                case 8:
                case 9:
                    if (highestScore < 5000) GSCFunctions.SetDynamicDvar("scr_war_scorelimit", 5000);
                    break;
                case 10:
                case 11:
                case 12:
                    if (highestScore < 7500) GSCFunctions.SetDynamicDvar("scr_war_scorelimit", 7500);
                    break;
            }
        }

        private static void initThrusterBehavior(Entity player)
        {
            if (GSCFunctions.GetDvar("g_thrusterType") == "1")
            {
                player.SetField("thrustersActive", 0);
                player.SetField("thrusterEnergy", 25);
                player.SetField("lastThrust", 0);
                player.SetField("mayThrust", 0);
                player.NotifyOnPlayerCommand("deactivateThrust", "-gostand");
                player.OnNotify("jumped", treyarchThrusters);
                thrusterHUD(player);

                player.OnNotify("deactivateThrust", (p) =>
                p.SetField("thrustersActive", 0));

                OnInterval(50, () =>
                {
                    if (!player.IsAlive) return true;

                    int lastThrust = player.GetField<int>("lastThrust");
                    int time = GSCFunctions.GetTime();
                    int thrusterEnergy = player.GetField<int>("thrusterEnergy");
                    if (time > lastThrust + 1500 && lastThrust != 0 && thrusterEnergy < 25)
                    {
                        if (player.GetField<int>("thrustersActive") == 1) return true;
                        player.SetField("thrusterEnergy", thrusterEnergy + 1);
                        updateThrusterBar(player, thrusterEnergy + 1);
                        //Log.Write(LogLevel.All, "Thruster energy updated to {0}", thrusterEnergy + 1);
                        return true;
                    }
                    else return true;
                });
            }
            else if (GSCFunctions.GetDvar("g_thrusterType") == "0")
            {
                player.OnNotify("jumped", shThrusters);
                player.OnNotify("hold_breath", shThrustersDirectional);
                player.OnNotify("adjustedStance", shThrusterSlam);
                player.SetField("hasThrustJumped", false);
                player.SetField("hasThrustedForward", false);
                player.SetField("mayThrust", false);
                player.SetField("maySlam", false);
                player.SetField("lastThrust", 0);
                player.SetField("thrusterEnergy", 3);

                OnInterval(1500, () =>
                {
                    int energy = player.GetField<int>("thrusterEnergy");
                    if (energy < 3)
                        player.SetField("thrusterEnergy", energy + 1);
                    if (player.IsPlayer) return true;
                    else return false;
                });
            }

        }

        private static void shThrusters(Entity player)
        {
            bool isGrounded = !player.GetField<bool>("mayThrust");
            bool hasThrustJumped = player.GetField<bool>("hasThrustJumped");
            bool hasThrusted = player.GetField<bool>("hasThrustedForward");
            int thrustsAvailable = player.GetField<int>("thrusterEnergy");
            if (!isGrounded && !hasThrustJumped && !hasThrusted && thrustsAvailable > 0 && player.IsAlive)
            {
                player.SetField("hasThrustJumped", true);
                player.SetField("thrusterEnergy", player.GetField<int>("thrusterEnergy") - 1);
                player.SetField("lastThrust", GSCFunctions.GetTime());
                AfterDelay(250, () => player.SetField("maySlam", true));
                if (player.HasPerk("specialty_quieter")) player.PlaySound("bullet_mega_flesh");
                else player.PlaySound("weap_hind_rocket_fire");
                shThrustRadarBlip(player);
                player.SetPerk("specialty_automantle", true, false);
                Vector3 currentVel = player.GetVelocity();
                float velZ = 500;
                player.SetVelocity(new Vector3(currentVel.X, currentVel.Y, velZ));
            }
            else if (!isGrounded && !hasThrustJumped && thrustsAvailable == 0 && player.IsAlive)
            {
                player.PlayLocalSound("weap_motiontracker_open_plr");
            }
            else
            {
                player.SetField("mayThrust", true);
                OnInterval(50, () =>
                {
                    bool grounded = player.IsOnGround();
                    if (grounded)
                    {
                        player.SetField("mayThrust", false);
                        player.SetField("maySlam", false);
                        player.SetField("hasThrustJumped", false);
                        player.SetField("hasThrustedForward", false);
                        player.UnSetPerk("specialty_automantle", true);
                        return false;
                    }
                    else return true;
                });
            }
        }
        private static void shThrustersDirectional(Entity player)
        {
            bool isGrounded = !player.GetField<bool>("mayThrust");
            bool hasThrusted = player.GetField<bool>("hasThrustedForward");
            int thrustsAvailable = player.GetField<int>("thrusterEnergy");
            int lastThrustTime = player.GetField<int>("lastThrust");
            int time = GSCFunctions.GetTime();
            Vector3 movement = player.GetNormalizedMovement();
            if (!isGrounded && !hasThrusted && thrustsAvailable > 0 && time > (lastThrustTime + 200) && player.IsAlive && movement.X > 0)
            {
                player.SetField("hasThrustedForward", true);
                player.SetPerk("specialty_automantle", true, false);
                player.SetField("thrusterEnergy", player.GetField<int>("thrusterEnergy") - 1);
                if (player.HasPerk("specialty_quieter")) player.PlaySound("bullet_mega_flesh");
                else player.PlaySound("weap_hind_rocket_fire");
                shThrustRadarBlip(player);
                Vector3 currentVel = player.GetVelocity();
                Vector3 angles = player.GetPlayerAngles();
                Vector3 forward = GSCFunctions.AnglesToForward(angles);
                //Log.Debug("X: {0}, Y: {1}, Z: {2}", forward.X, forward.Y, forward.Z);
                Vector3 newVel = new Vector3(currentVel.X + (forward.X * 250), currentVel.Y + (forward.Y * 250), currentVel.Z);

                player.SetVelocity(newVel);
            }
            else if (!hasThrusted && thrustsAvailable > 0 && time > (lastThrustTime + 200) && player.IsAlive && movement.Y != 0 && movement.X == 0)//Dodge
            {
                player.SetField("hasThrustedForward", true);
                player.SetPerk("specialty_automantle", true, false);
                player.SetField("thrusterEnergy", player.GetField<int>("thrusterEnergy") - 1);
                if (player.HasPerk("specialty_quieter")) player.PlaySound("bullet_mega_flesh");
                else player.PlaySound("weap_hind_rocket_fire");
                shThrustRadarBlip(player);
                Vector3 currentVel = player.GetVelocity();
                Vector3 angles = player.GetPlayerAngles();
                Vector3 right = GSCFunctions.AnglesToRight(angles);
                //Log.Debug("X: {0}, Y: {1}, Z: {2}", forward.X, forward.Y, forward.Z);
                Vector3 newVel;
                if (movement.Y > 0) newVel = new Vector3(currentVel.X + (right.X * 300), currentVel.Y + (right.Y * 300), currentVel.Z);
                else newVel = new Vector3(currentVel.X + (-right.X * 300), currentVel.Y + (-right.Y * 300), currentVel.Z);

                player.SetVelocity(newVel);
                player.SlideVelocity += newVel;
                AfterDelay(1000, () => player.SetField("hasThrustedForward", false));
            }
            else if (!hasThrusted && thrustsAvailable == 0)
            {
                player.PlayLocalSound("weap_motiontracker_open_plr");
            }
        }
        private static void shThrusterSlam(Entity player)
        {
            bool isGrounded = !player.GetField<bool>("mayThrust");
            bool hasThrustJumped = player.GetField<bool>("hasThrustJumped");
            //bool hasThrusted = player.GetField<bool>("hasThrustedForward");
            bool maySlam = player.GetField<bool>("maySlam");
            bool isCrouching = player.GetStance() == "crouch";
            if (!isGrounded && hasThrustJumped && maySlam && isCrouching && player.IsAlive)
            {
                player.SetField("hasThrustedForward", true);
                player.SetField("maySlam", false);
                player.PlaySound("ims_rocket_fire_npc");
                //Vector3 currentVel = player.GetVelocity();
                Vector3 angles = player.GetPlayerAngles();
                Vector3 forward = GSCFunctions.AnglesToForward(angles);
                Vector3 newVel = new Vector3((forward.X * 100), (forward.Y * 100), -100);

                player.SetVelocity(newVel);

                //Monitor landing
                OnInterval(50, () =>
                {
                    bool grounded = player.IsOnGround();
                    if (!player.IsAlive) return false;
                    if (grounded)
                    {
                        player.RadiusDamage(player.Origin, 45, 150, 50, player, "MOD_CRUSH", "destructible_toy");
                        return false;
                    }
                    else return true;
                });
            }
        }
        private static void shThrustRadarBlip(Entity player)
        {
            if (player.HasPerk("specialty_quieter")) return;//No blip with blast suppressor

            player.SetPerk("specialty_radarblip", true, false);
            AfterDelay(300, () => player.UnSetPerk("specialty_radarblip", true));

            /*
            foreach (Entity players in Players)
            {
                if (!players.IsPlayer) continue;

                bool hadAssassin = players.HasPerk("specialty_coldblooded");

                if (players != player) players.SetPerk("specialty_coldblooded", true, false);

                Entity blip = GSCFunctions.Spawn("script_model", player.Origin);
                blip.SetModel("tag_origin");
                blip.MakePortableRadar(players);
                AfterDelay(1900, () =>
                {
                    blip.Delete();
                    if (!hadAssassin) players.UnSetPerk("specialty_coldblooded");
                });
            }
            */
        }

        private static void treyarchThrusters(Entity player)
        {
            bool isGrounded = player.GetField<int>("mayThrust") == 0;
            if (!isGrounded)
            {
                player.SetField("thrustersActive", 1);
                Entity thrusterSound = null;
                if (!player.HasPerk("specialty_quieter") && player.GetField<int>("thrusterEnergy") > 0)
                {
                    thrusterSound = GSCFunctions.Spawn("script_origin", player.Origin);
                    //thrusterSound.LinkTo(player);
                    thrusterSound.PlayLoopSound("weap_hind_rocket_loop_dist");

                    player.SetPerk("specialty_radarblip", true, false);
                }

                OnInterval(50, () =>
                {
                    int thrusterEnergy = player.GetField<int>("thrusterEnergy");
                    if (thrusterEnergy <= 0)
                    {
                        player.SetField("thrustersActive", 0);
                        return true;
                    }
                    Vector3 currentVel = player.GetVelocity();
                    float vel = currentVel.Z += 75;
                    if (vel > 200) vel = 200;
                    player.SetVelocity(new Vector3(currentVel.X, currentVel.Y, vel));
                    player.SetField("thrusterEnergy", thrusterEnergy - 1);
                    updateThrusterBar(player, thrusterEnergy - 1);

                    if (thrusterSound != null) thrusterSound.Origin = player.Origin;

                    //Log.Write(LogLevel.All, "Energy {0}", bird.GetField<int>("thrusterEnergy"));
                    thrusterEnergy = player.GetField<int>("thrusterEnergy");
                    int time = GSCFunctions.GetTime();
                    player.SetField("lastThrust", time);
                    bool stopFlying = (thrusterEnergy == 0 || player.GetField<int>("thrustersActive") == 0);
                    if (stopFlying)
                    {
                        if (!player.HasPerk("specialty_quieter") && thrusterSound != null)
                        {
                            thrusterSound.StopLoopSound();
                            thrusterSound.Delete();
                            player.UnSetPerk("specialty_radarblip", true);
                        }
                        return false;
                    }
                    else return true;
                });
            }
            else
            {
                player.SetField("mayThrust", 1);
                OnInterval(50, () =>
                {
                    bool grounded = player.IsOnGround();
                    if (grounded)
                    {
                        player.SetField("mayThrust", 0);
                        return false;
                    }
                    else return true;
                });
            }
        }

        private static bool checkForWallRun(Entity player)
        {
            if (!player.IsOnGround()) return true;

            Vector3 playerAngles = player.Angles;//player.GetPlayerAngles();
            Vector3 playerOrigin = player.Origin;
            Vector3 right = GSCFunctions.AnglesToRight(playerAngles);
            Vector3 rightPosition = playerOrigin + (right * 5);
            Vector3 leftPosition = playerOrigin + (right * -5);
            bool wallR = !GSCFunctions.PhysicsTrace(playerOrigin, rightPosition).Equals(rightPosition) && GSCFunctions.SightTracePassed(playerOrigin, rightPosition, false);
            bool wallL = !GSCFunctions.PhysicsTrace(playerOrigin, leftPosition).Equals(leftPosition) && GSCFunctions.SightTracePassed(playerOrigin, leftPosition, false);
            Log.Write(LogLevel.All, "L {0} R {1}", wallL, wallR);

            if (player.IsAlive) return true;
            else return false;
        }

        private static void thrusterHUD(Entity player)
        {
            HudElem thrusterBarBG = HudElem.CreateIcon(player, "white", 105, 5);
            thrusterBarBG.Alpha = 0;
            thrusterBarBG.SetPoint("center", "center", 0, 50);
            thrusterBarBG.HideWhenInMenu = true;
            thrusterBarBG.Archived = true;
            thrusterBarBG.Foreground = false;
            HudElem thrusterBar = HudElem.CreateIcon(player, "line_horizontal", 100, 3);
            thrusterBar.Alpha = 0;
            //thrusterBar.AlignX = "center";
            //thrusterBar.AlignY = "center";
            thrusterBar.SetPoint("center", "center", 0, 50);
            thrusterBar.HideWhenInMenu = true;
            thrusterBar.Archived = true;
            thrusterBar.Foreground = true;
            thrusterBar.Parent = thrusterBarBG;
            player.SetField("hud_thrusterBar", new Parameter(thrusterBar));
        }
        private static void updateThrusterBar(Entity player, int percentage)
        {
            HudElem thrusterBar = player.GetField<HudElem>("hud_thrusterBar");
            thrusterBar.FadeOverTime(1);
            thrusterBar.Alpha = 1;
            thrusterBar.Parent.FadeOverTime(.5f);
            thrusterBar.Parent.Alpha = .2f;
            thrusterBar.ScaleOverTime(.5f, percentage*4, 3);
            AfterDelay(2000, () =>
                {
                    int thrusterEnergy = player.GetField<int>("thrusterEnergy");
                    if (thrusterEnergy >= 25)
                    {
                        //HudElem thrusterBar = p.GetField<HudElem>("hud_thrusterBar");
                        if (thrusterBar.Alpha == 1)
                        {
                            thrusterBar.FadeOverTime(.5f);
                            thrusterBar.Alpha = 0;
                            thrusterBar.Parent.FadeOverTime(1);
                            thrusterBar.Parent.Alpha = 0;
                        }
                    }
                });
        }
    }
}
