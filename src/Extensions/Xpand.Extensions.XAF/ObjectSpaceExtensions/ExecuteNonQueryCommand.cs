﻿using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions;

public static partial class ObjectSpaceExtensions {
    public static int ExecuteNonQueryCommand(this IObjectSpace objectSpace, string commandText) {
        using var command = objectSpace.CreateCommand();
        command.CommandText = commandText;
        return command.ExecuteNonQuery();
    }
}