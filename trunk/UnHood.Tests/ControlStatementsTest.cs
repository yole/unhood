using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnHood.Engine;

namespace UnHood.Tests
{
    [TestFixture]
    public class ControlStatementsTest
    {
        [Test]
        public void ForEachSimple()
        {
            var builder = new StatementListBuilder();
            builder
                .AddForeach(0, 52, "Outer.DynamicActors(Class'Actor', A)")
                .Add(25, "I++")
                .Add(32, "LogInternal(I @ A))")
                .AddIteratorNext(51)
                .AddIteratorPop(52)
                .Add(53, "LogInternal(\"Num dynamic actors: \" $ I)")
                .AddReturn(87);
            VerifyText(builder,
                       "foreach Outer.DynamicActors(Class'Actor', A)",
                       "{",
                       "    I++;",
                       "    LogInternal(I @ A));",
                       "}",
                       "LogInternal(\"Num dynamic actors: \" $ I);");
        }

        [Test]
        public void IfInForeach()
        {
            var builder = new StatementListBuilder();
            builder
                .AddForeach(175, 238, "Outer.DynamicActors(Class'Actor', A)")
                .AddJumpIfNot(200, 237, "ClassIsChildOf(A.Class, aClass)")
                .Add(225, "A.Destroy()")
                .AddIteratorNext(237)
                .AddIteratorPop(238)
                .AddReturn(239);
            VerifyText(builder,
                "foreach Outer.DynamicActors(Class'Actor', A)",
                "{",
                "    if (ClassIsChildOf(A.Class, aClass))",
                "    {",
                "        A.Destroy();",
                "    }",
                "}");
        }

        [Test]
        public void BreakInIfInForeach()
        {
            var builder = new StatementListBuilder();
            builder
                .AddForeach(0, 99, "Outer.WorldInfo.AllControllers(Class'Controller', P)")
                .AddJumpIfNot(37, 98, "P.bIsPlayer && P.PlayerReplicationInfo.GetPlayerAlias() ~= S")
                .AddJump(95, 99)
                .AddIteratorNext(98)
                .AddIteratorPop(99)
                .AddReturn(100);
            VerifyText(builder,
                "foreach Outer.WorldInfo.AllControllers(Class'Controller', P)",
                "{",
                "    if (P.bIsPlayer && P.PlayerReplicationInfo.GetPlayerAlias() ~= S)",
                "    {",
                "        break;",
                "    }",
                "}");
        }

        [Test]
        public void Continue()
        {
            var builder = new StatementListBuilder();
            builder
                .AddForeach(0, 136, "Outer.WorldInfo.AllControllers(Class'PlayerController', PC)")
                .AddJumpIfNot(37, 135, "PC.bIsPlayer && PC.IsLocalPlayerController()")
                .Add(79, "DCC = (DebugCameraController) PC")
                .AddJumpIfNot(95, 132, "DCC != None && DCC.OryginalControllerRef == None")
                .AddIteratorNext(128)
                .AddJump(129, 136)
                .AddJump(132, 136)
                .AddIteratorNext(135)
                .AddIteratorPop(136)
                .AddReturn(137);
            VerifyText(builder,
                "foreach Outer.WorldInfo.AllControllers(Class'PlayerController', PC)",
                "{",
                "    if (PC.bIsPlayer && PC.IsLocalPlayerController())",
                "    {",
                "        DCC = (DebugCameraController) PC;",
                "        if (DCC != None && DCC.OryginalControllerRef == None)",
                "        {",
                "            continue;",
                "        }",
                "        break;",
                "    }",
                "}");
        }

        [Test]
        public void ReturnInForeach()
        {
            var builder = new StatementListBuilder();
            builder
                .AddForeach(0, 106, "Outer.AllActors(Class'Actor', A)")
                .AddJumpIfNot(25, 105, "A.Name == actorName")
                .Add(49, "Outer.SetViewTarget(A)")
                .AddIteratorPop(102)
                .AddReturn(103)
                .AddIteratorNext(105)
                .AddIteratorPop(106)
                .AddReturn(107);
            VerifyText(builder,
                "foreach Outer.AllActors(Class'Actor', A)",
                "{",
                "    if (A.Name == actorName)",
                "    {",
                "        Outer.SetViewTarget(A);",
                "        return;",
                "    }",
                "}");
        }

        [Test]
        public void While()
        {
            var builder = new StatementListBuilder();
            builder
                .AddJumpIfNot(0, 182, "Len(Text) > 0")
                .Add(13, "Character = Asc(Left(Text, 1))")
                .Add(29, "Text = Mid(Text, 1)")
                .AddJumpIfNot(44, 179, "Character >= 32 && Character < 256 && Character != Asc(\"~\") && Character != Asc(\"`\")")
                .Add(107, "SetInputText(Left(TypedStr, TypedStrPos) $ Chr(Character) $ Right(TypedStr, Len(TypedStr) - TypedStrPos))")
                .Add(161, "SetCursorPos(TypedStrPos + 1)")
                .AddJump(179, 0)
                .AddReturn(182);

            VerifyText(builder,
                "while (Len(Text) > 0)",
                "{",
                "    Character = Asc(Left(Text, 1));",
                "    Text = Mid(Text, 1);",
                "    if (Character >= 32 && Character < 256 && Character != Asc(\"~\") && Character != Asc(\"`\"))",
                "    {",
                "        SetInputText(Left(TypedStr, TypedStrPos) $ Chr(Character) $ Right(TypedStr, Len(TypedStr) - TypedStrPos));",
                "        SetCursorPos(TypedStrPos + 1);",
                "    }",
                "}");
        }

        [Test]
        public void Switch()
        {
            var builder = new StatementListBuilder();
            builder
                .AddSwitch(0, "Physics")
                .AddCase(7, "0")
                .AddReturn(12, "\"None\"")
                .AddJump(19, 232)
                .AddDefaultCase(229)
                .AddReturn(232, "\"Unknown\"");

            VerifyText(builder,
                "switch (Physics)",
                "{",
                "    case 0:",
                "        return \"None\";",
                "}",
                "return \"Unknown\";");
        }

        [Test]
        public void SwitchNoBreak()
        {
            var builder = new StatementListBuilder();
            builder
                .AddSwitch(0, "SplitscreenType")
                .AddCase(7, "0")
                .AddCase(12, "2")
                .AddReturn(17, "true")
                .AddDefaultCase(72)
                .AddReturn(75, "false")
                .AddErrorReturn(77, "// invalid");

            VerifyText(builder,
                "switch (SplitscreenType)",
                "{",
                "    case 0:",
                "    case 2:",
                "        return true;",
                "    default:",
                "        return false;",
                "}");
        }

        [Test]
        public void IfElseReturn()
        {
            var builder = new StatementListBuilder();
            builder
                .AddJumpIfNot(0, 112, "!bGameRelevant")
                .AddJumpIfNot(87, 109, "bNoDelete")
                .Add(96, "ShutDown()")
                .AddJump(106, 112)
                .Add(109, "Destroy()")
                .AddReturn(112);

            VerifyText(builder,
                       "if (!bGameRelevant)",
                       "{",
                       "    if (bNoDelete)",
                       "    {",
                       "        ShutDown();",
                       "    }",
                       "    else",
                       "    {",
                       "        Destroy();",
                       "    }",
                       "}");
        }

        [Test]
        public void BreakInElseInForeach()
        {
            var builder = new StatementListBuilder();
            builder
                .AddForeach(320, 438, "LocalPlayerControllers(Class'PlayerController', P)")
                .AddJumpIfNot(339, 437, "P.ViewTarget != None")
                .AddJumpIfNot(359, 402, "P.Pawn == Instigator && Instigator != None")
                .AddIteratorPop(396)
                .AddReturn(397, "true")
                .AddJump(399, 437)
                .Add(402, "bResult = CheckMaxEffectDistance(P, SpawnLocation, CullDistance)")
                .AddJump(434, 438)
                .AddIteratorNext(437)
                .AddIteratorPop(438)
                .AddReturn(439, "bResult");
            VerifyText(builder,
                "foreach LocalPlayerControllers(Class'PlayerController', P)",
                "{",
                "    if (P.ViewTarget != None)",
                "    {",
                "        if (P.Pawn == Instigator && Instigator != None)",
                "        {",
                "            return true;",
                "        }",
                "        else",
                "        {",
                "            bResult = CheckMaxEffectDistance(P, SpawnLocation, CullDistance);",
                "            break;",
                "        }",
                "    }",
                "}",
                "return bResult;");
        }

        [Test]
        public void BreakInWhile()
        {
            var builder = new StatementListBuilder();
            builder
                .AddJumpIfNot(11, 180, "CurrentMove != None")
                .AddJumpIfNot(22, 174, "CurrentMove.TimeStamp <= CurrentTimeStamp")
                .Add(70, "LastAckedAccel = CurrentMove.Acceleration")
                .AddJump(171, 177)
                .AddJump(174, 180)
                .AddJump(177, 11)
                .AddReturn(180);
            VerifyText(builder,
                "while (CurrentMove != None)",
                "{",
                "    if (CurrentMove.TimeStamp <= CurrentTimeStamp)",
                "    {",
                "        LastAckedAccel = CurrentMove.Acceleration;",
                "    }",
                "    else",
                "    {",
                "        break;",
                "    }",
                "}");
        }

        [Test]
        public void ReturnInElse()
        {
            var builder = new StatementListBuilder();
            builder
                .Add(0, "P = Pawn(TheActor)")
                .AddJumpIfNot(16, 36, "P != None")
                .AddReturn(27, "P")
                .AddJump(33, 81)
                .Add(36, "C = Controller(TheActor)")
                .AddReturn(52, "C != None ? C.Pawn : None");
            VerifyText(builder,
                "P = Pawn(TheActor);",
                "if (P != None)",
                "{",
                "    return P;",
                "}",
                "else",
                "{",
                "    C = Controller(TheActor);",
                "    return C != None ? C.Pawn : None;",
                "}");
        }

        private static string PrintText(StatementListBuilder builder)
        {
            builder.List.CreateControlStatements();
            builder.List.RemoveRedundantReturns();
            var result = new TextBuilder();
            builder.List.Print(result, null, false);
            return result.ToString();
        }

        private static void VerifyText(StatementListBuilder builder, params string[] lines)
        {
            string text = PrintText(builder);
            var expected = new StringBuilder();
            foreach (var line in lines)
            {
                expected.Append("    ").Append(line).Append("\r\n");
            }
            Assert.AreEqual(expected.ToString(), text);
        }

    }
}
