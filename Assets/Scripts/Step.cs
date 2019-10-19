// this is one step of a editing process
public enum StepType
{
	create,
	destroy
}

public class Step
{
	private StepType type;
	private DEPosition rfu; //the rfu of selection range
	private DEPosition lbd; //the lbd of selection range
	private DEPosition beginPos;
	private DEDirection beginDir;
	private DEPosition endPos;
	private DEDirection endDir;

	private DEPosition[] units;

	public Step(StepType stepType, DEPosition[] containUnits, DEPosition beginPos, DEDirection beginDir, DEPosition endPos, DEDirection endDir)
	{
		this.units = new DEPosition[] { };
		this.beginPos = new DEPosition();
		this.endPos = new DEPosition();
		this.units = containUnits;
		this.beginPos = beginPos;
		this.beginDir = beginDir;
		this.endPos = endPos;
		this.endDir = endDir;
		this.type = stepType;
	}

	public Step(StepType stepType, DEPosition beginPos, DEDirection beginDir, DEPosition endPos, DEDirection endDir)
	{
		this.beginPos = new DEPosition();
		this.endPos = new DEPosition();

		type = stepType;
		this.beginPos = beginPos;
		this.beginDir = beginDir;
		this.endPos = endPos;
		this.endDir = endDir;
	}

	public Step(StepType stepType, DEPosition rfu, DEPosition lbd)
	{
		type = stepType;
		this.rfu = rfu;
		this.lbd = lbd;
	}

	public DEPosition[] GetSelectionRange()
	{
		return new DEPosition[] { rfu, lbd };
	}

	public StepType GetStepType() { return type; }

	public DEPosition GetBeginPos() { return this.beginPos; }

	public DEPosition GetEndPos() { return this.endPos; }

	public DEDirection GetBeginDir() { return this.beginDir; }

	public DEDirection GetEndDir() { return this.endDir; }

	public DEPosition[] GetUnits() { return units; }
}
