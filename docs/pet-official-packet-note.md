# Official pet packet follow-up

The current `sc_p` serializer should be compared against a fresh packet capture from the official client before changing its field list again.

A modern reference implementation places the pet attack type immediately after `Experience`, followed by attack upgrade, damage, hit, critical, defence, resistances, HP/MP, team state and required level XP. NosGm currently also carries project-specific training fields after the traditional final field.

Those extra fields are not changed in this combat fix because the reported 0% progression has a stronger demonstrated cause in target acquisition and participation. A future packet correction must use an actual official `sc_p` capture from the same client version to avoid replacing one incompatible layout with another.
