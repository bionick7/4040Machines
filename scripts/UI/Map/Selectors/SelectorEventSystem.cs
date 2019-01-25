using UnityEngine;

public class SelectorEventSystem
{

	/*
	/// <summary> Called, if the pointer hovers over a selectorbutton </summary>
	/// <param name="concerned"> The selectorbutton in question </param>
	/// <param name="enter"> If the pointer enters or exits the button </param>
	public void SelectorLikeHovered (SelectorButton concerned, bool enter) {
		switch (concerned.parent.identifyer) {
		case SelectorIdentifyer.none:
			return;
		case SelectorIdentifyer.target_parts:
			if (enter) {
				ShipPart shippart = concerned.attached as ShipPart;
				SceneGlobals.ui_script.HighlightingPosition = SceneGlobals.map_camera.WorldToScreenPoint(shippart.Transform.position);
			} else {
				SceneGlobals.ui_script.HighlightingPosition = new Vector3(-200, -200);
			}
			return;
		case SelectorIdentifyer.attack:
			if (enter) {
				Target tgt = concerned.attached as Target;
				SceneGlobals.ui_script.HighlightingPosition = SceneGlobals.map_camera.WorldToScreenPoint(tgt.Position);
			} else {
				SceneGlobals.ui_script.HighlightingPosition = new Vector3(-200, -200);
			}
			return;
		case SelectorIdentifyer.turret_attack:
			if (enter) {
				Target tgt = concerned.attached as Target;
				SceneGlobals.ui_script.HighlightingPosition = SceneGlobals.map_camera.WorldToScreenPoint(tgt.Position);
			} else {
				SceneGlobals.ui_script.HighlightingPosition = new Vector3(-200, -200);
			}
			return;
		case SelectorIdentifyer.match_velocity:
			if (enter) {
				Target tgt = concerned.attached as Target;
				SceneGlobals.ui_script.HighlightingPosition = SceneGlobals.map_camera.WorldToScreenPoint(tgt.Position);
			} else {
				SceneGlobals.ui_script.HighlightingPosition = new Vector3(-200, -200);
			}
			return;
		case SelectorIdentifyer.match_velocity_closest:
			if (enter) {
				Target tgt = concerned.attached as Target;
				SceneGlobals.ui_script.HighlightingPosition = SceneGlobals.map_camera.WorldToScreenPoint(tgt.Position);
			} else {
				SceneGlobals.ui_script.HighlightingPosition = new Vector3(-200, -200);
			}
			return;
		}
	}

	/// <summary> Called, if the pointer clicks on a selectorbutton </summary>
	/// <param name="concerned"> The selectorbutton in question </param>
	public void SelectorLikeClicked (SelectorButton concerned) {
		foreach (Ship concerned_ship in concerned.parent.parent_objs) {
			Target tgt = concerned.attached as Target;
			switch (concerned.parent.identifyer) {
			case SelectorIdentifyer.target_parts:
// Target Parts --------------------------------------------------------------------------------------------------------------
				ShipPart shippart = concerned.attached as ShipPart;
				SceneGlobals.Player.TurretAim = shippart;
				goto LOOPEND;
			case SelectorIdentifyer.attack:
// Attack --------------------------------------------------------------------------------------------------------------------
				concerned_ship.low_ai.Attack(tgt);
				goto LOOPEND;
			case SelectorIdentifyer.turret_attack:
// Turret Attack --------------------------------------------------------------------------------------------------------------------
				concerned_ship.low_ai.TurretAttack(tgt);
				goto LOOPEND;
			case SelectorIdentifyer.match_velocity:
// Match Velocity ------------------------------------------------------------------------------------------------------------
				concerned_ship.low_ai.MatchVelocity(tgt);
				goto LOOPEND;
			case SelectorIdentifyer.match_velocity_closest:
// Match Velocity Closest ----------------------------------------------------------------------------------------------------
				concerned_ship.low_ai.MatchVelocityNearTarget(tgt);
				goto LOOPEND;
			default:
			case SelectorIdentifyer.none:
				goto LOOPEND;
			}
			LOOPEND:;
		}
	}
	*/

	public int lasting_command;

	public IMarkerParentObject [] concerned_objects = new IMarkerParentObject [0];

	/// <summary>
	///		Should be called, if the pin label is dragget to a part in response to a selector event
	/// </summary>
	/// <param name="aimable"> The aimable, where the pointer lands </param>
	public void DraggedTo (IAimable aimable) {

		/*
		Debug.Log("-----------");
		Debug.Log(lasting_command.ToString("x"));
		Debug.Log(concerned_objects);
		*/

		Target tgt = Target.None;
		if (aimable is ITargetable) {
			tgt = ((ITargetable) aimable).Associated;
		}

		switch (lasting_command) {
		case 0x39:

			// Match Velocity Closest

			if (tgt.is_none) {
				foreach (Ship concerned in System.Array.ConvertAll(concerned_objects, x => x is Target ? ((Target) x).Ship : x as Ship)) {
					Debug.Log(concerned);
					if (concerned != null) {
						concerned.low_ai.MatchVelocityNearTarget(tgt);
						Debug.Log(concerned.low_ai.ShowCommands());
					}
				}
			}
			break;
		case 0x3a:

			// Match Velocity

			if (tgt.is_none) {
				foreach (Ship concerned in System.Array.ConvertAll(concerned_objects, x => x as Ship)) {
					if (concerned != null) {
						concerned.low_ai.MatchVelocity(tgt);
					}
				}
			}
			break;
		case 0x3b:

			// Attack

			if (tgt.is_none) {
				foreach (Ship concerned in System.Array.ConvertAll(concerned_objects, x => x as Ship)) {
					if (concerned != null) {
						concerned.low_ai.Attack(tgt);
					}
				}
			}
			break;
		case 0x3c:

			// Turret Attack

			if (tgt.is_none) {
				foreach (Ship concerned in System.Array.ConvertAll(concerned_objects, x => x as Ship)) {
					if (concerned != null) {
						concerned.low_ai.TurretAttack(tgt);
					}
				}
			}
			break;
		case 0x3e:

			// Target Part

			foreach (Ship concerned in System.Array.ConvertAll(concerned_objects, x => x as Ship)) {
				if (concerned != null) {
					concerned.TurretAim = aimable;
				}
			}
			break;
		default:
			break;
		}
		concerned_objects = new IMarkerParentObject [0];
		lasting_command = -1;
	}

	public void SelectorClicked (SelectorButton button) {
		SubSelector concerned = button.parent as SubSelector;
		if (concerned == null) return;
		byte option = button.num;

		bool multiple = concerned.targets.Length > 1;
		for (int i=0; i < concerned.targets.Length; i++) {
			// Check Flags
			if (i != 0 && (button.flags >> 1) % 2 == 0)
				// single vessel only
				goto LOOPEND;
			if (i == 0 && button.flags % 2 == 1) {
				// use pinlabel
				Debug.Log(DeveloppmentTools.LogIterable(concerned.targets));
				concerned_objects = concerned.targets;
				lasting_command = button.function;
			}

			IMarkerParentObject parentObject = concerned.targets[i];
			var ship = parentObject as Ship;

			SelectorOptions options = concerned.options;

			switch (button.function) {
//  +-----------------------------------------------------------------------------------------------------------------------+
//	|									Reference																			|
//  +-----------------------------------------------------------------------------------------------------------------------+
			case 0x0c:
// Set Camera ---------------------------------------------------------------------------------------------------------------
				SceneGlobals.ReferenceSystem.Offset += parentObject.Position - SceneGlobals.ReferenceSystem.Position;
				goto LOOPEND;
			case 0x0d:
// Set Reference ------------------------------------------------------------------------------------------------------------
				SceneGlobals.map_core.CurrentSystem = new ReferenceSystem(parentObject.Position);
				goto LOOPEND;
			case 0x0e:
// Lock Reference -----------------------------------------------------------------------------------------------------------
				Vector3 offset = Vector3.zero;
				SceneObject scene_obj = parentObject as SceneObject;
				if (parentObject is SceneObject) {
					offset = SceneGlobals.ReferenceSystem.Position - scene_obj.Position;
					SceneGlobals.ReferenceSystem = new ReferenceSystem(scene_obj);
				}
				else if (parentObject is ITargetable) {
					offset = SceneGlobals.ReferenceSystem.Position - parentObject.Position;
					Target tgt = (parentObject as ITargetable).Associated;
					SceneGlobals.ReferenceSystem = new ReferenceSystem(tgt);
				}

				SceneGlobals.ReferenceSystem.Offset = offset;
				goto LOOPEND;
			case 0x0f:
// Lock & Set ---------------------------------------------------------------------------------------------------------------
				if (parentObject is SceneObject) {
					SceneGlobals.ReferenceSystem = new ReferenceSystem(parentObject as SceneObject);
				}
				else if (parentObject is ITargetable) {
					SceneGlobals.ReferenceSystem = new ReferenceSystem((parentObject as ITargetable).Associated);
				}
				goto LOOPEND;
			case 0x19:
// Match Velocity Closest ---------------------------------------------------------------------------------------------------
					if (parentObject is ITargetable) {
						foreach (IMarkerParentObject impo in MapCore.Active.selection) {
							Ship ship01 = impo as Ship;
							if (ship01 != null && ship01.Friendly) {
								ship01.low_ai.MatchVelocityNearTarget((parentObject as ITargetable).Associated);
							}
						}
					}
					goto LOOPEND;
			case 0x1a:
// Match Velocity -----------------------------------------------------------------------------------------------------------
				if (parentObject is IPhysicsObject) {
					foreach (IMarkerParentObject impo in MapCore.Active.selection) {
						Ship ship01 = impo as Ship;
						if (ship01 != null && ship01.Friendly) {
							ship01.low_ai.MatchVelocity((parentObject as ITargetable).Associated);
						}
					}
				}
				goto LOOPEND;
			case 0x1b:
// Attack -------------------------------------------------------------------------------------------------------------------
				if (parentObject is ITargetable) {
					Target self_tgt = (parentObject as ITargetable).Associated;
					foreach (IMarkerParentObject impo in MapCore.Active.selection) {
						Ship ship01 = impo as Ship;
						if (ship01 != null && ship01.Friendly) {
							ship01.low_ai.Attack(self_tgt);
						}
					}
				}
				goto LOOPEND;
			case 0x1c:
// TurretAttack -------------------------------------------------------------------------------------------------------------
				if (parentObject is ITargetable) {
					Target self_tgt = (parentObject as ITargetable).Associated;
					foreach (IMarkerParentObject impo in MapCore.Active.selection) {
						Ship ship01 = impo as Ship;
						if (ship01 != null && ship01.Friendly) {
							ship01.low_ai.TurretAttack(self_tgt);
						}
					}
				}
				goto LOOPEND;
			case 0x1d:
// Aim Here -----------------------------------------------------------------------------------------------------------------
				SceneGlobals.Player.TurretAim = parentObject;
				goto LOOPEND;
			case 0x1e:
// Target Part --------------------------------------------------------------------------------------------------------------

				goto LOOPEND;
			case 0x1f:
// Set Target ---------------------------------------------------------------------------------------------------------------
				if (parentObject is ITargetable) {
					Target tgt = (parentObject as ITargetable).Associated;
					SceneGlobals.Player.Target = tgt;
				}
				goto LOOPEND;
//  +-----------------------------------------------------------------------------------------------------------------------+
//  |										Info																			|
//  +-----------------------------------------------------------------------------------------------------------------------+
			case 0x2e:
// Ship Information ---------------------------------------------------------------------------------------------------------
				if (i != 0) goto LOOPEND;
				concerned.SpawnChild("Ship Information", new string [] {
					ship.Mass.ToString("Mass: 0.0 t"),
					string.Format("HP: {0:0.0} / {1:0.0}", ship.HP, ship.tot_hp),
				});
				goto LOOPEND;
			case 0x2f:
// OS -----------------------------------------------------------------------------------------------------------------------
				goto LOOPEND;
//  +-----------------------------------------------------------------------------------------------------------------------+
//  |									Command																				|
//  +-----------------------------------------------------------------------------------------------------------------------+

// Match Velocity Closest ---------------------------------------------------------------------------------------------------
			case 0x29:

				goto LOOPEND;
// Match Velocity -----------------------------------------------------------------------------------------------------------
			case 0x3a:

				goto LOOPEND;
			case 0x3b:
// Flee ---------------------------------------------------------------------------------------------------------------------
				ship.low_ai.Flee();
				goto LOOPEND;
			case 0x3c:
// Idle ---------------------------------------------------------------------------------------------------------------------
				ship.low_ai.Idle();
				goto LOOPEND;
			case 0x3d:
// Attack -------------------------------------------------------------------------------------------------------------------

				goto LOOPEND;
			case 0x3e:
// TurretAttack -------------------------------------------------------------------------------------------------------------

				goto LOOPEND;
			case 0x3f:
// Control ------------------------------------------------------------------------------------------------------------------
				if (parentObject is Ship) {
					SceneGlobals.Player = parentObject as Ship;
				}
				goto LOOPEND;
			default: goto LOOPEND;

			}
			LOOPEND:;
		}
	}

/*
	public void SubSelectorClicked (SelectorButton button) {
		SubSelector concerned = button.parent_subsel;
		byte option = parent_subsel.options.Place(label);

		bool multiple = concerned.targets.Length > 1;
		for (int i=0; i < concerned.targets.Length; i++) {
			IMarkerParentObject parentObject = concerned.targets[i];
			var ship = parentObject as Ship;

			SelectorOptions options = concerned.options;
			switch (options.selection_class) {
			case SelectionClass.Reference:
//  +-----------------------------------------------------------------------------------------------------------------------+
//	|									Reference																			|
//  +-----------------------------------------------------------------------------------------------------------------------+
				if (multiple) goto LOOPEND;
				switch (option) {
				case 0xc:
// Set Camera ---------------------------------------------------------------------------------------------------------------
					SceneGlobals.ReferenceSystem.Offset += parentObject.Position - SceneGlobals.ReferenceSystem.Position;
					goto LOOPEND;
				case 0xd:
// Set Reference -----------------------------------------------------------------------------------------------------------
					SceneGlobals.map_core.CurrentSystem = new ReferenceSystem(parentObject.Position);
					goto LOOPEND;
				case 0xe:
// Lock Reference -----------------------------------------------------------------------------------------------------------
					Vector3 offset = Vector3.zero;
					SceneObject scene_obj = parentObject as SceneObject;
					if (parentObject is SceneObject) {
						offset = SceneGlobals.ReferenceSystem.Position - scene_obj.Position;
						SceneGlobals.ReferenceSystem = new ReferenceSystem(scene_obj);
					}
					else if (parentObject is ITargetable) {
						offset = SceneGlobals.ReferenceSystem.Position - parentObject.Position;
						Target tgt = (parentObject as ITargetable).Associated;
						SceneGlobals.ReferenceSystem = new ReferenceSystem(tgt);
					}

					SceneGlobals.ReferenceSystem.Offset = offset;
					goto LOOPEND;
				case 0xf:
// Lock & Set -----------------------------------------------------------------------------------------------------------
					if (parentObject is SceneObject) {
						SceneGlobals.ReferenceSystem = new ReferenceSystem(parentObject as SceneObject);
					}
					else if (parentObject is ITargetable) {
						SceneGlobals.ReferenceSystem = new ReferenceSystem((parentObject as ITargetable).Associated);
					}
					goto LOOPEND;
				default: goto LOOPEND;
				}
			case SelectionClass.Target:
//  +-----------------------------------------------------------------------------------------------------------------------+
//	|									Target																				|
//  +-----------------------------------------------------------------------------------------------------------------------+
				if (multiple) goto LOOPEND;
				switch (option) {
				case 0x9:
// Match Velocity Closest ---------------------------------------------------------------------------------------------------
					if (parentObject is ITargetable) {
						foreach (IMarkerParentObject impo in MapCore.Active.selection) {
							Ship ship01 = impo as Ship;
							if (ship01 != null && ship01.Friendly) {
								ship01.low_ai.MatchVelocityNearTarget((parentObject as ITargetable).Associated);
							}
						}
					}
					goto LOOPEND;
				case 0xa:
// Match Velocity -----------------------------------------------------------------------------------------------------------
					if (parentObject is IPhysicsObject) {
						foreach (IMarkerParentObject impo in MapCore.Active.selection) {
							Ship ship01 = impo as Ship;
							if (ship01 != null && ship01.Friendly) {
								ship01.low_ai.MatchVelocity((parentObject as ITargetable).Associated);
							}
						}
					}
					goto LOOPEND;
				case 0xb:
// Attack -------------------------------------------------------------------------------------------------------------------
					if (parentObject is ITargetable) {
						Target self_tgt = (parentObject as ITargetable).Associated;
						foreach (IMarkerParentObject impo in MapCore.Active.selection) {
							Ship ship01 = impo as Ship;
							if (ship01 != null && ship01.Friendly) {
								ship01.low_ai.Attack(self_tgt);
							}
						}
					}
					goto LOOPEND;
				case 0xc:
// TurretAttack -------------------------------------------------------------------------------------------------------------
					if (parentObject is ITargetable) {
						Target self_tgt = (parentObject as ITargetable).Associated;
						foreach (IMarkerParentObject impo in MapCore.Active.selection) {
							Ship ship01 = impo as Ship;
							if (ship01 != null && ship01.Friendly) {
								ship01.low_ai.TurretAttack(self_tgt);
							}
						}
					}
					goto LOOPEND;
				case 0xd:
// Aim Here -----------------------------------------------------------------------------------------------------------------
					SceneGlobals.Player.TurretAim = parentObject;
					goto LOOPEND;
				case 0xe:
// Target Part --------------------------------------------------------------------------------------------------------------
					SelectorLike child_Te = concerned.SpawnChild("Target Part",
						System.Array.ConvertAll(ship.Parts.AllParts, p => p.Description()),
						ship.Parts.AllParts
					);
					child_Te.identifyer = SelectorIdentifyer.target_parts;
					child_Te.parent_objs = concerned.targets;
					goto LOOPEND;
				case 0xf:
// Set Target ---------------------------------------------------------------------------------------------------------------
					if (parentObject is ITargetable) {
						Target tgt = (parentObject as ITargetable).Associated;
						SceneGlobals.Player.Target = tgt;
					}
					goto LOOPEND;
				default: goto LOOPEND;
				}
			case SelectionClass.Info:
//  +-----------------------------------------------------------------------------------------------------------------------+
//	|										Info																			|
//  +-----------------------------------------------------------------------------------------------------------------------+
				if (multiple) goto LOOPEND;
				switch (option) {
				case 0xe:
// Ship Information ---------------------------------------------------------------------------------------------------------
					if (i != 0) goto LOOPEND;
					concerned.SpawnChild("Ship Information", new string [] {
						ship.Mass.ToString("Mass: 0.0 t"),
						string.Format("HP: {0:0.0} / {1:0.0}", ship.HP, ship.tot_hp),
					});
					goto LOOPEND;
				case 0xf:
// OS -----------------------------------------------------------------------------------------------------------------------
					goto LOOPEND;
				default: goto LOOPEND;
				}
			default:
			case SelectionClass.Command:
//  +-----------------------------------------------------------------------------------------------------------------------+
//	|									Command																				|
//  +-----------------------------------------------------------------------------------------------------------------------+
				string[] final_options = new string[0];
				Target[] final_ship_arr = new Target[0];

				// List all the objects in the scene
				if ((option == 0xb || option == 0xd || option == 0xe || option == 0xa || option == 0x9) && i == 0) {
					bool only_ennemies = option == 0xe;
					int ship_num = SceneGlobals.ship_collection.Count, destroyables_num = SceneGlobals.destroyables.Count;

					Target[] ennemytargets = new Target[ship_num + destroyables_num - 1];
					Ship[] ship_arr = new Ship[ship_num];
					DestroyableTarget[] destroyable_arr = new DestroyableTarget[destroyables_num];
					SceneGlobals.ship_collection.CopyTo(ship_arr);
					SceneGlobals.destroyables.CopyTo(destroyable_arr);

					bool include_curret_target = !SceneGlobals.Player.Target.is_none;

					System.Array.ConvertAll(System.Array.FindAll(ship_arr, x => x != ship), x => x.Associated).CopyTo(ennemytargets, 0);
					System.Array.ConvertAll(destroyable_arr, x => x.Associated).CopyTo(ennemytargets, ship_num - 1);

					ennemytargets = System.Array.FindAll(ennemytargets, x => only_ennemies ? x.Friendly ^ ship.Friendly :true);

					final_options = new string[ennemytargets.Length + (include_curret_target ? 1 : 0)];
					final_ship_arr = new Target[ennemytargets.Length + (include_curret_target ? 1 : 0)];
					ennemytargets.CopyTo(final_ship_arr, (include_curret_target ? 1 : 0));
					System.Array.ConvertAll(ennemytargets, x => x.Name).CopyTo(final_options, (include_curret_target ? 1 : 0));
					if (include_curret_target) {
						final_options [0] = "Current target";
						final_ship_arr [0] = SceneGlobals.Player.Target;
					}
				}
				switch (option) {
// Match Velocity Closest ---------------------------------------------------------------------------------------------------
				case 0x9:
					if (i != 0) goto LOOPEND;
					SelectorLike child_Ca = concerned.SpawnChild("Match Velocity Closest",
						final_options,
						final_ship_arr
					);
					child_Ca.identifyer = SelectorIdentifyer.match_velocity_closest;
					child_Ca.parent_objs = concerned.targets;
					goto LOOPEND;
// Match Velocity -----------------------------------------------------------------------------------------------------------
				case 0xa:
					if (i != 0) goto LOOPEND;
					SelectorLike child_Cb = concerned.SpawnChild("Match Velocity",
						final_options,
						final_ship_arr
					);
					child_Cb.identifyer = SelectorIdentifyer.match_velocity;
					child_Cb.parent_objs = concerned.targets;
					goto LOOPEND;
				case 0xb:
// Flee ---------------------------------------------------------------------------------------------------------------------
					ship.low_ai.Flee();
					goto LOOPEND;
				case 0xc:
// Idle ---------------------------------------------------------------------------------------------------------------------
					ship.low_ai.Idle();
					goto LOOPEND;
				case 0xd:
// Attack -------------------------------------------------------------------------------------------------------------------
					if (i != 0) goto LOOPEND;

					SelectorLike child_Cd = concerned.SpawnChild("Attack",
						final_options,
						final_ship_arr
					);
					child_Cd.identifyer = SelectorIdentifyer.attack;
					child_Cd.parent_objs = concerned.targets;
					goto LOOPEND;
				case 0xe:
// TurretAttack -------------------------------------------------------------------------------------------------------------------
					if (i != 0) goto LOOPEND;

					SelectorLike child_Ce = concerned.SpawnChild("TurretAttack",
						final_options,
						final_ship_arr
					);
					child_Ce.identifyer = SelectorIdentifyer.turret_attack;
					child_Ce.parent_objs = concerned.targets;
					goto LOOPEND;
				case 0xf:
// Control ------------------------------------------------------------------------------------------------------------------
					if (parentObject is Ship) {
						SceneGlobals.Player = parentObject as Ship;
					}
					goto LOOPEND;
				default: goto LOOPEND;
				}
			}
			LOOPEND:;
		}
	}
*/
}